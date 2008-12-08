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
	/// A IFileSelector that selects files by explicit list of names 
	/// </summary>
	public class ListFileSelector : IFileSelector
	{
		protected string[] Names;
	
		public ListFileSelector(params string[] names)
		{
			Names = new string[names.Length];
			// normalize file names
			for (int i=0; i<Names.Length; i++)
				Names[i] = names[i].Replace( Path.AltDirectorySeparatorChar, Path.DirectorySeparatorChar );
		}
		
		public bool IncludeFile(IFileObject file) {
			string normFileName = file.Name.Replace( Path.AltDirectorySeparatorChar, Path.DirectorySeparatorChar );
			return Array.IndexOf( Names, normFileName )>=0;
		}
		
		public bool TraverseDescendents(IFileObject file) {
			// TODO: more intellectual behaviour should be implemented here
			return true;
		}		
		
	}
}
