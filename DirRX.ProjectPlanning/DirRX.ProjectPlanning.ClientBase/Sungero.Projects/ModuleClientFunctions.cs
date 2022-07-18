using System;
using System.Collections.Generic;
using System.Linq;
using Sungero.Core;
using Sungero.CoreEntities;

namespace DirRX.ProjectPlanning.Module.Projects.Client
{
	partial class ModuleFunctions
	{

		public override void CreateDocument()
		{
			ProjectDocuments.CreateDocumentWithCreationDialog(ProjectDocuments.Info,
                                                        Sungero.Docflow.SimpleDocuments.Info,
                                                        Sungero.Docflow.Addendums.Info,
                                                        Sungero.Docflow.MinutesBases.Info,
                                                        DirRX.ProjectPlanner.ProjectPlans.Info
                                                       );
		}
	}
}