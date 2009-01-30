﻿#region License
/*
 * Open NIC.NET library (http://nicnet.googlecode.com/)
 * Copyright 2004-2008 NewtonIdeas
 * Distributed under the LGPL licence
 *
 * Unless required by applicable law or agreed to in writing, software
 * distributed under the License is distributed on an "AS IS" BASIS,
 * WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
 * See the License for the specific language governing permissions and
 * limitations under the License.
 */
#endregion

using System;
using System.Collections;
using System.Data;
using System.Collections.Generic;
using System.Reflection;
using System.Linq;
using System.Linq.Expressions;
using System.Text;
using NI.Common.Providers;

namespace NI.Data.Dalc.Linq
{
	public class QueryProvider : System.Linq.IQueryProvider {
		string SourceName;
		IDalc Dalc;

		public QueryProvider(string sourceName, IDalc dalc)
		{
			SourceName = sourceName;
			Dalc = dalc;
		}

		public IQueryable<TElement> CreateQuery<TElement>(Expression expression)
		{
			return new DalcData<TElement>(this, expression);
		}

		public IQueryable CreateQuery(Expression expression)
		{
			var type = expression.Type;
			if (!type.IsGenericType)
				throw new Exception("Unknown expression type");
			var genericType = type.GetGenericTypeDefinition();
			if (genericType == typeof(IQueryable<>) || genericType == typeof(IOrderedQueryable<>))
				type = type.GetGenericArguments()[0];

			try {
				return (IQueryable)Activator.CreateInstance(
						typeof(DalcData<>).MakeGenericType(type),
						new object[] { this, expression });
			} catch (System.Reflection.TargetInvocationException tie) {
				throw tie.InnerException;
			}
		}

		public TResult Execute<TResult>(Expression expression)
		{
			Query q = new Query(SourceName);
			BuildDalcQuery(q, expression);

			object result = null;
			IQueryProvider dalcQueryPrv = new NI.Data.Dalc.QueryProvider(new ConstObjectProvider(q));
			if (q.RecordCount == 1 && q.Fields != null && q.Fields.Length == 1) {
				DalcObjectProvider prv = new DalcObjectProvider() { Dalc = Dalc, QueryProvider = dalcQueryPrv };
				result = prv.GetObject(null);
			} else if (q.RecordCount == 1) {
				DalcRecordDictionaryProvider prv = new DalcRecordDictionaryProvider() { Dalc = Dalc, QueryProvider = dalcQueryPrv };
				result = prv.GetDictionary(null);
			} else {
				DalcDictionaryListProvider prv = new DalcDictionaryListProvider() { Dalc = Dalc, QueryProvider = dalcQueryPrv };
				result = prv.GetDictionaryList(null);
			}
			// now lets try to convert 

			Type resT = typeof(TResult);
			if (resT == typeof(IEnumerable) && (result is IEnumerable || result == null)) {
				return result!=null ? (TResult)result : (TResult)((object)new IDictionary[0]);
			}

			if (resT.IsGenericType && resT.GetGenericTypeDefinition() == typeof(IEnumerable<>)) {
				Type[] genArgs = resT.GetGenericArguments();
				int arrLen = result is IList ? ((ICollection)result).Count : 1;
				Array resArr = Array.CreateInstance(genArgs[0], arrLen);
				if (result is IList) {
					IList resultList = (IList)result;
					for (int i = 0; i < resultList.Count; i++)
						resArr.SetValue( PrepareResult(resultList[i],genArgs[0]), i );
				} else {
					resArr.SetValue(PrepareResult(result, genArgs[0]), 0);
				}
				return (TResult)((object)resArr);
			}

			return (TResult)PrepareResult(result, typeof(TResult));
		}

		protected object PrepareResult(object o, Type t) {
			if (t.IsInstanceOfType(o))
				return o;
			if (o == null && !t.IsValueType)
				return o;
			if (t == typeof(DalcRecord) && (o is IDictionary))
				return new DalcRecord((IDictionary)o);
			if (t == typeof(DalcValue))
				return new DalcValue(o);
			throw new InvalidCastException();
		}

		public object Execute(Expression expression)
		{
			return Execute<IEnumerable>(expression);
		}

		protected void ApplySingleOrDefault(Query q, MethodCallExpression call) {
			BuildDalcQuery(q, call.Arguments[0]);
			q.RecordCount = 1;
		}

		protected void ApplyLinq(Query q, MethodCallExpression call) {
			ConstantExpression sourceNameConst = (ConstantExpression)call.Arguments[1];
			q.SourceName = sourceNameConst.Value.ToString();
		}

		protected void ApplyWhere(Query q, MethodCallExpression call) {
			BuildDalcQuery(q, call.Arguments[0]);
			q.Root = ComposeCondition(call.Arguments[1]);
			if (call.Arguments[1] is UnaryExpression) {
				UnaryExpression unExpr = (UnaryExpression)call.Arguments[1];
				if (unExpr.Operand is LambdaExpression) {
					LambdaExpression lambdaExpr = (LambdaExpression)unExpr.Operand;
					if (lambdaExpr.Parameters.Count == 1)
						q.SourceName += "." + lambdaExpr.Parameters[0].Name;
				}
			}
		}

		protected void ApplySelect(Query q, MethodCallExpression call) {
			BuildDalcQuery(q, call.Arguments[0]);
			q.Fields = new string[] { ComposeFieldValue(call.Arguments[1]).Name };
		}

		protected void ApplyOrderBy(Query q, MethodCallExpression call) {
			BuildDalcQuery(q, call.Arguments[0]);
			AddQuerySort(q, ComposeFieldValue(call.Arguments[1]).Name);
		}

		protected void ApplyOrderByDescending(Query q, MethodCallExpression call) {
			BuildDalcQuery(q, call.Arguments[0]);
			AddQuerySort(q, ComposeFieldValue(call.Arguments[1]).Name + " " + QSortField.Desc);
		}

		protected void ApplyThenBy(Query q, MethodCallExpression call) {
			ApplyOrderBy(q, call);
		}

		protected void ApplyThenByDescending(Query q, MethodCallExpression call) {
			ApplyOrderByDescending(q, call);
		}


		protected void BuildDalcQuery(Query q, Expression expression) {
			if (expression is MethodCallExpression) {
				MethodCallExpression call = (MethodCallExpression)expression;
				MethodInfo applyMethod = this.GetType().GetMethod("Apply" + call.Method.Name,
											BindingFlags.Instance | BindingFlags.NonPublic, null,
											new Type[] { typeof(Query), typeof(MethodCallExpression) }, null);
				if (applyMethod==null)
					throw new NotSupportedException();
				applyMethod.Invoke(this, new object[] { q, call });
			}
		}

		protected void AddQuerySort(Query q, string sortFld) {
			if (q.Sort != null) {
				string[] newSort = new string[q.Sort.Length + 1];
				Array.Copy(q.Sort, newSort, q.Sort.Length);
				newSort[q.Sort.Length] = sortFld;
				q.Sort = newSort;
			} else {
				q.Sort = new string[] { sortFld };
			}
		}

		protected IQueryNode ComposeCondition(Expression expression) {
			if (expression is UnaryExpression) {
				UnaryExpression unExpr = (UnaryExpression)expression;
				IQueryNode qNode = ComposeCondition(unExpr.Operand);
				return (unExpr.NodeType==ExpressionType.Not ? new QueryNegationNode(qNode) : qNode );
			}
			if (expression is LambdaExpression) {
				LambdaExpression lambdaExpr = (LambdaExpression)expression;
				return ComposeCondition(lambdaExpr.Body);
			}
			if (expression is BinaryExpression) {
				BinaryExpression binExpr = (BinaryExpression)expression;
				if (binExpr.NodeType == ExpressionType.AndAlso || binExpr.NodeType == ExpressionType.OrElse) {
					QueryGroupNode qGroup = new QueryGroupNode(binExpr.NodeType == ExpressionType.AndAlso ? GroupType.And : GroupType.Or);
					qGroup.Nodes.Add(ComposeCondition(binExpr.Left));
					qGroup.Nodes.Add(ComposeCondition(binExpr.Right));
					return qGroup;
				}
				if (conditionMapping.ContainsKey(binExpr.NodeType)) {
					Conditions qCond = conditionMapping[binExpr.NodeType];
					QueryConditionNode qCondNode = new QueryConditionNode(
							ComposeValue(binExpr.Left), qCond, ComposeValue(binExpr.Right) );
					return qCondNode;
				}
			}
			else if (expression is MethodCallExpression) {
				MethodCallExpression methodExpr = (MethodCallExpression)expression;
				// check for special method call like 'In' or 'Like'
				if (methodExpr.Method.Name == "In") { 
					IQueryFieldValue fldValue = ComposeFieldValue(methodExpr.Object);
					IQueryValue inValue = ComposeValue(methodExpr.Arguments[0]);
					// possible conversion to IList
					if (inValue is IQueryConstantValue) {
						IQueryConstantValue inConstValue = (IQueryConstantValue)inValue;
						if (!(inConstValue.Value is IList)) {
							IList constList = new ArrayList();
							foreach (object o in ((IEnumerable)inConstValue.Value))
								constList.Add(o);
							if (constList.Count==0) // means 'nothing'?
								return new QueryConditionNode( (QConst)"1", Conditions.Equal, (QConst)"2" );
							inValue = new QConst(constList);
						}
					}
					return new QueryConditionNode(fldValue, Conditions.In, inValue);
				} else if (methodExpr.Method.Name == "Like") {
					IQueryFieldValue fldValue = ComposeFieldValue(methodExpr.Object);
					IQueryValue likeValue = ComposeValue(methodExpr.Arguments[0]);
					return new QueryConditionNode(fldValue, Conditions.Like, likeValue);
				}
			}

			throw new NotSupportedException();
		}

		static IDictionary<ExpressionType, Conditions> conditionMapping;
		
		static QueryProvider() {
			conditionMapping = new Dictionary<ExpressionType,Conditions>();
			conditionMapping[ExpressionType.Equal] = Conditions.Equal;
			conditionMapping[ExpressionType.NotEqual] = Conditions.Equal|Conditions.Not;
			conditionMapping[ExpressionType.GreaterThan] = Conditions.GreaterThan;
			conditionMapping[ExpressionType.GreaterThanOrEqual] = Conditions.GreaterThan|Conditions.Equal;
			conditionMapping[ExpressionType.LessThan] = Conditions.LessThan;
			conditionMapping[ExpressionType.LessThanOrEqual] = Conditions.LessThan | Conditions.Equal;
		}

		protected IQueryFieldValue ComposeFieldValue(Expression expression) {
			IQueryValue fldValue = ComposeValue(expression);
			if (fldValue is IQueryFieldValue)
				return (IQueryFieldValue)fldValue;
			else
				throw new NotSupportedException();
		}

		protected bool IsDalcQueryExpression(Expression expression) {
			if (expression is MethodCallExpression) {
				MethodCallExpression mCall = (MethodCallExpression)expression;
				if (mCall.Method.Name == "Linq")
					return true;
				if (mCall.Arguments.Count > 0)
					return IsDalcQueryExpression(mCall.Arguments[0]);
			}
			return false;
		}

		protected IQueryValue ComposeValue(Expression expression) {
			if (expression is UnaryExpression) {
				UnaryExpression unExpr = (UnaryExpression)expression;
				return ComposeValue(unExpr.Operand);
			}
			if (expression is LambdaExpression) {
				LambdaExpression lambdaExpr = (LambdaExpression)expression;
				return ComposeValue(lambdaExpr.Body);
			}
			if (expression is MethodCallExpression) {
				MethodCallExpression methodExpr = (MethodCallExpression)expression;
				if (methodExpr.Method.Name == "get_Item") {
					if (methodExpr.Arguments.Count == 1 && 
						methodExpr.Arguments[0] is ConstantExpression &&
						methodExpr.Object is ParameterExpression) {
						ConstantExpression fldNameExpr = (ConstantExpression)methodExpr.Arguments[0];
						// lets extract prefix
						ParameterExpression paramExpr = (ParameterExpression)methodExpr.Object;
						string fldName = fldNameExpr.Value.ToString();
						if (fldName.IndexOf('(') < 0) // not function - tmp hack! TODO fix aliases
							fldName = paramExpr.Name + "." + fldName;
						return new QField(fldName);
					}
				} else if (methodExpr.Method.Name == "Select" && IsDalcQueryExpression(methodExpr) ) {
					Query nestedQ = new Query(String.Empty);
					BuildDalcQuery(nestedQ, methodExpr);
					if (!String.IsNullOrEmpty(nestedQ.SourceName))
						return nestedQ;
					throw new NotSupportedException();
				}
				
			}

			if (expression is ConstantExpression) {
				ConstantExpression constExpr = (ConstantExpression)expression;
				return new QConst(constExpr.Value);
			}

			// just try to eval
			LambdaExpression lExpr = Expression.Lambda(expression);
			return new QConst(lExpr.Compile().DynamicInvoke(null));
		}

    }
}
