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
using System.Data;

namespace NI.Data
{

	public interface IDataSetFactory {
		
		/// <summary>
		/// Get empty DataSet instance with schema for specified table name
		/// </summary>
		/// <param name="tableName">name of table</param>
		/// <returns>DataSet with schema</returns>
		DataSet GetDataSet(string tableName);
	}

	
}
