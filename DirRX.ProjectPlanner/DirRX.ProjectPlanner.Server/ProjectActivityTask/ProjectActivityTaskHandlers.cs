using System;
using System.Collections.Generic;
using System.Linq;
using Sungero.Core;
using Sungero.CoreEntities;
using DirRX.ProjectPlanner.ProjectActivityTask;

namespace DirRX.ProjectPlanner
{
  partial class ProjectActivityTaskServerHandlers
  {

    public override void BeforeStart(Sungero.Workflow.Server.BeforeStartEventArgs e)
    {
      if (_obj.Responsibles == null || _obj.Responsibles.Count == 0)
      {
        e.AddError(DirRX.ProjectPlanner.ProjectActivityTasks.Resources.ResponsibleRequired);
      }
      
      if (_obj.MaxDeadline == null || Calendar.Today > _obj.MaxDeadline)
      {
        e.AddError(DirRX.ProjectPlanner.ProjectActivityTasks.Resources.IncorrectDeadline);
      }
    }
  }


}