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

namespace NI.Data
{
	/// <summary>
	/// Represents abstract SQL builder interface.
	/// </summary>
	public interface ISqlBuilder
	{
		/// <summary>
		/// Build string representation of specified IQueryValue
		/// </summary>
		string BuildValue(IQueryValue v);

		/// <summary>
		/// Build string representation of specified sort field
		/// </summary>
		string BuildSort(QSort sortFld);

		/// <summary>
		/// Build string representation of specified QueryNode (condition)
		/// </summary>
		string BuildExpression(QueryNode node);
	}
}
