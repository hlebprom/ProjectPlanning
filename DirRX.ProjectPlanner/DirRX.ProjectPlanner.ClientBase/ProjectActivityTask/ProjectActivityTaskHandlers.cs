using System;
using System.Collections.Generic;
using System.Linq;
using Sungero.Core;
using Sungero.CoreEntities;
using DirRX.ProjectPlanner.ProjectActivityTask;

namespace DirRX.ProjectPlanner
{
  partial class ProjectActivityTaskClientHandlers
  {

    public override void Showing(Sungero.Presentation.FormShowingEventArgs e)
    {
      _obj.State.Properties.ProjectActivity.IsEnabled = false;
      _obj.State.Properties.ProjectPlan.IsEnabled = false;
      _obj.State.Properties.Responsibles.IsEnabled = _obj.State.Properties.Subject.IsEnabled;
    }

  }
}