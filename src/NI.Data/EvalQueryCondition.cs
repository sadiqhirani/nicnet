#region License
/*
 * Open NIC.NET library (http://nicnet.googlecode.com/)
 * Copyright 2004-2012 NewtonIdeas
 * Copyright 2008-2013 Vitalii Fedorchenko (changes and v.2)
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
using System.Text;

using NI.Data;

namespace NI.Data {
	
	/// <summary>
	/// Object query condition evaluator. Evaluates IQueryNode condition in some 'object' context.
	/// </summary>
	/// <remarks>This resolver does not support all possible IQueryNode tree structures.</remarks>
	public class EvalQueryCondition {

		/// <summary>
		/// Field value resolver (required)
		/// </summary>
		public Func<ResolveNodeContext, object> QFieldResolver { get; set; }

		/// <summary>
		/// Const value resolver (optional)
		/// </summary>
		public Func<ResolveNodeContext, object> QConstResolver { get; set; }
		
		public object Evaluate(IDictionary context, QueryNode condition) {
			if (condition==null)
				return true;
			return EvaluateInternal(context, condition);
		}

		protected object ResolveNodeValue(ResolveNodeContext nodeContext) {
			if (nodeContext.Node is QConst)
				return QConstResolver!=null ? QConstResolver(nodeContext) : ((QConst)nodeContext.Node).Value;
			if (nodeContext.Node is QField)
				return QFieldResolver!=null ? QFieldResolver(nodeContext) : nodeContext.Context[ ((QField)nodeContext.Node).Name ];
			throw new Exception("Cannot resolve value node type: " + nodeContext.Node.GetType().ToString());
		}
		
		protected bool EvaluateInternal(IDictionary context, QueryNode node) {
			if (node is QueryConditionNode) {
				var condNode = (QueryConditionNode)node;
				
				IQueryValue lValue = condNode.LValue;
				IQueryValue rValue = condNode.RValue;
				
				bool negate = (condNode.Condition & Conditions.Not)==Conditions.Not;
				bool isLike = (condNode.Condition & Conditions.Like) == Conditions.Like;
				bool isIn = (condNode.Condition & Conditions.In) == Conditions.In;
				bool isNull = (condNode.Condition & Conditions.Null) == Conditions.Null;
				
				ResolveNodeContext lValueContext = new ResolveNodeContext(lValue,rValue,context);
				ResolveNodeContext rValueContext = new ResolveNodeContext(rValue, lValue, context);
				
				bool compareResult = true;
				if (!isLike && !isIn && !isNull) {
					var leftVal = ResolveNodeValue(lValueContext);
					var rightVal = ResolveNodeValue(rValueContext);
					var cmpResult = Compare(leftVal, rightVal);

					compareResult = false;
					if ((condNode.Condition & Conditions.Equal) == Conditions.Equal && cmpResult == 0) {
						compareResult = true;
					}
					if ((condNode.Condition & Conditions.GreaterThan) == Conditions.GreaterThan && cmpResult > 0) {
						compareResult = true;
					}
					if ((condNode.Condition & Conditions.LessThan) == Conditions.LessThan && cmpResult < 0) {
						compareResult = true;
					}
					if ((condNode.Condition & Conditions.Not) == Conditions.Not)
						compareResult = !compareResult;
					
				} else if (isLike) {
					string lString = Convert.ToString(ResolveNodeValue(lValueContext));
					string rString = Convert.ToString(ResolveNodeValue(rValueContext));
					bool startWildcard = rString.StartsWith("%");
					bool endWildcard = rString.EndsWith("%");
					if (startWildcard)
						rString = rString.Substring(1);
					if (endWildcard)
						rString = rString.Substring(0, rString.Length-1);
					
					if (startWildcard && endWildcard) {
						compareResult = lString.Contains(rString);
					} else if (startWildcard) {
						compareResult = lString.EndsWith(rString);
					} else {
						compareResult = lString.StartsWith(rString);
					}
				} else if (isIn) {
					object lObj = ResolveNodeValue(lValueContext);
					object rObj = ResolveNodeValue(rValueContext);
					if (!(rObj is IList))
						throw new Exception("Condition 'In' expects IList as right operand");
					compareResult = ((IList)rObj).Contains(lObj);
				} else if (isNull) {
					object lObj = ResolveNodeValue(lValueContext);
					compareResult = lObj==null || lObj==DBNull.Value;
				} else {
					throw new Exception("Cannot apply conditions: "+condNode.Condition.ToString() );
				}
				
				return negate ? !compareResult : compareResult;
		
			}
			if (node is QueryGroupNode) {
				var groupNode = (QueryGroupNode)node;
				bool groupResult = groupNode.GroupType == QueryGroupNodeType.And ? true : false;
				foreach (QueryNode groupChildNode in groupNode.Nodes) {
					bool childResult = EvaluateInternal(context, groupChildNode);
					if (groupNode.GroupType==QueryGroupNodeType.And)
						groupResult = groupResult && childResult;
					if (groupNode.GroupType == QueryGroupNodeType.Or)
						groupResult = groupResult || childResult;
				}
				return groupResult;
			}
			throw new Exception("Cannot resolve query node type: "+node.GetType().ToString() );
		}


		protected int Compare(object a, object b) {
			return DbValueComparer.Instance.Compare(a,b);
		}
		
		public class ResolveNodeContext {
			IQueryValue _Node;
			IQueryValue _CompareNode;
			IDictionary _Context;

			public IQueryValue Node { get { return _Node; } }
			public IQueryValue CompareNode { get { return _CompareNode; } }
			public IDictionary Context { get { return _Context; } }

			public ResolveNodeContext(IQueryValue node, IQueryValue compareNode, IDictionary context) {
				_Node = node;
				_CompareNode = compareNode;
				_Context = context;
			}
		}
		
	}
}
