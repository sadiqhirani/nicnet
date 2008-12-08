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
using System.Reflection;

using NI.Common;

namespace NI.Winter {

	/// <summary>
	/// Property invoking factory component
	/// </summary>
	public class PropertyInvokingFactory : Component, IFactoryComponent {
		object _TargetObject;
		string _TargetProperty;
	
		/// <summary>
		/// Get or set target object
		/// </summary>
		[Dependency]
		public object TargetObject {
			get { return _TargetObject; }
			set { _TargetObject = value; }
		}
		
		/// <summary>
		/// Get or set static target property name
		/// </summary>
		[Dependency]
		public string TargetProperty {
			get { return _TargetProperty; }
			set { _TargetProperty = value; }
		}

		
		
		public PropertyInvokingFactory() {
		}
		
		public object GetObject() {
			Type targetType = TargetObject.GetType();
			
			System.Reflection.PropertyInfo pInfo = targetType.GetProperty( TargetProperty, BindingFlags.Instance|BindingFlags.Public);
			if (pInfo==null)
				throw new MissingMemberException( targetType.ToString(), TargetProperty);
			return pInfo.GetValue( TargetObject, null );
		}
		
		public Type GetObjectType() {
			Type targetType = TargetObject.GetType();

			System.Reflection.PropertyInfo pInfo = targetType.GetProperty( TargetProperty, BindingFlags.Instance|BindingFlags.Public);
			if (pInfo==null)
				throw new MissingMemberException( targetType.ToString(), TargetProperty);
			return pInfo.PropertyType;
		}		
		
		
	}
}
