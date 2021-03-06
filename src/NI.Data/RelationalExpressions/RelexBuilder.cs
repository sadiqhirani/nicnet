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
using System.Linq;
using System.Collections;
using System.Globalization;
using System.Text;
using NI.Data;

namespace NI.Data.RelationalExpressions {
	
	public class RelexBuilder {
		
		public string BuildRelex(QueryNode node) {
			InternalBuilder builder = new InternalBuilder();
			return builder.BuildExpression(node);
		}

        public string BuildRelex(Query node) {
            InternalBuilder builder = new InternalBuilder();
            return builder.BuildQueryString(node, false);
        }
		
		class InternalBuilder : SqlBuilder {

			public override string BuildExpression(QueryNode node) {
				if (node is Query)
					return BuildQueryString((Query)node, false);
				return base.BuildExpression(node);
			}

			protected override string BuildGroup(QueryGroupNode node) {
				var grp = base.BuildGroup(node);
				if (!String.IsNullOrEmpty( node.Name ) ) {
					return String.Format("(<{0}> {1})", node.Name, grp);
				} else return grp;
				
			}


			public string BuildQueryString(Query q, bool isNested) {
				string rootExpression = BuildExpression(q.Condition);
				if (rootExpression != null && rootExpression.Length > 0)
					rootExpression = String.Format("({0})", rootExpression);
				string fieldExpression = q.Fields != null ? 
					String.Join(",", q.Fields.Select(v=>(string)v).ToArray() ) : "*";
				if (q.Sort != null && q.Sort.Length > 0) {
					fieldExpression = String.Format("{0};{1}", fieldExpression, 
						String.Join(",", q.Sort.Select(v=>(string)v).ToArray()));
				}
				string limitExpression = isNested || (q.StartRecord==0 && q.RecordCount==Int32.MaxValue) ? 
					String.Empty : String.Format("{{{0},{1}}}", q.StartRecord, q.RecordCount);
				return String.Format("{0}{1}[{2}]{3}", q.Table, rootExpression,
					fieldExpression, limitExpression);
			}

			static readonly string[] stringConditions = new string[] {
					"=", ">", ">=", "<", "<=", " in ", " like ", "="
			};
			static readonly Conditions[] enumConditions = new Conditions[] {
					Conditions.Equal, Conditions.GreaterThan, 
					Conditions.GreaterThan|Conditions.Equal,
					Conditions.LessThan, Conditions.LessThan|Conditions.Equal,
					Conditions.In, Conditions.Like, Conditions.Null
			};

			protected override string BuildCondition(QueryConditionNode node) {
				string lvalue = BuildValue(node.LValue);
				string rvalue = BuildValue(node.RValue);
				Conditions condition = (node.Condition | Conditions.Not) ^ Conditions.Not;
				string res = null;
				for (int i=0; i<enumConditions.Length; i++)
					if (enumConditions[i]==condition) {
						res = stringConditions[i];
						break; // first match
					}
				if (res==null)
					throw new ArgumentException("Invalid conditions set", condition.ToString());
				if ((node.Condition & Conditions.Not)==Conditions.Not)
					res = "!" + res;
				if ((node.Condition & Conditions.Null) == Conditions.Null)
					rvalue = "null";
				string result = String.Format("{0}{1}{2}", lvalue, res, rvalue);
				if ( !String.IsNullOrEmpty( node.Name ) )
					result = String.Format("(<{0}> {1})", node.Name, result);
				return result;
			}
			

			public override string BuildValue(IQueryValue value) {
				if (value is Query)
					return BuildQueryString((Query)value, true);
				if (value is QRawSql)
					return BuildValue(((QRawSql)value).SqlText) + ":sql";
				return base.BuildValue(value);
			}

			protected override string BuildValue(QConst qConst) {
				object constValue = qConst.Value;
				if (constValue == null)
					return "null";
				
				// special processing for arrays
				if (constValue is IList)
					return BuildValue((IList)constValue);
				if (constValue is string && qConst.Type==TypeCode.String)
					return BuildValue((string)constValue);

				TypeCode constTypeCode = qConst.Type;
				string typeSuffix = constTypeCode!=TypeCode.Empty && constTypeCode!=TypeCode.DBNull ? ":"+constTypeCode.ToString() : String.Empty;
				return BuildValue( Convert.ToString(constValue, CultureInfo.InvariantCulture ) ) + typeSuffix;
			}

			protected override string BuildValue(IList list) {
				string[] paramNames = new string[list.Count];
				// in relexes only supported arrays that can be represented as comma-delimeted string 
				for (int i = 0; i < list.Count; i++)
					paramNames[i] = Convert.ToString(list[i]);
				return BuildValue( String.Join(",", paramNames) ) + ":string[]"; // TODO: array type suggestion logic!
			}			
			
			protected override string BuildValue(string str) {
				return "\""+str.Replace("\"", "\"\"")+"\"";
			}			

		}		
		
	}
}
