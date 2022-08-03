using System;
using System.Collections.Generic;
using System.Linq;
using Sungero.Core;
using Sungero.CoreEntities;
using Sungero.Domain.Initialization;

namespace DirRX.ProjectPlanning.Module.Projects.Server
{
	public partial class ModuleInitializer
	{

		public override void Initializing(Sungero.Domain.ModuleInitializingEventArgs e)
		{
			base.Initializing(e);
			GrantRightsOnProjectFolder();
		}
		
    /// <summary>
    /// Выдача прав на папку проекта.
    /// </summary>
		public void GrantRightsOnProjectFolder()
		{
			var allUsers = Roles.AllUsers;
			DirRX.ProjectPlanning.Module.Projects.SpecialFolders.ProjectsPlansDirRX.AccessRights.Grant(allUsers, DefaultAccessRightsTypes.Read);
			DirRX.ProjectPlanning.Module.Projects.SpecialFolders.ProjectsPlansDirRX.AccessRights.Save();
		}
	}
}
