#region License
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
using System.IO;

namespace NI.Vfs
{
	/// <summary>
	/// Represents the data content of a file.
	/// </summary>
	public interface IFileContent
	{
		/// <summary>
		/// Returns the file which this is the content of.
		/// </summary>
		IFileObject File { get; }
		
		/// <summary>
		/// Returns an input stream for reading the file's content.
		/// </summary>
		Stream InputStream { get; }

		/// <summary>
		/// Returns an output stream for writing the file's content.
		/// </summary>
		Stream OutputStream { get; }
		
		/// <summary>
		/// Determines the size of the file, in bytes.
		/// </summary>
		long Size { get; }
		
		/// <summary>
		/// Get or set the last-modified timestamp of the file.
		/// </summary>
		DateTime LastModifiedTime { get; set; }
		
		/// <summary>
		/// Closes all resources used by the content, including any open stream.
		/// </summary>
		void Close();
	}
}
