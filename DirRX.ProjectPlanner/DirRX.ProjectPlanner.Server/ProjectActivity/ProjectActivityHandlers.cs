using System;
using System.Collections.Generic;
using System.Linq;
using Sungero.Core;
using Sungero.CoreEntities;
using DirRX.ProjectPlanner.ProjectActivity;

namespace DirRX.ProjectPlanner
{
  partial class ProjectActivityServerHandlers
  {

    public override void Created(Sungero.Domain.CreatedEventArgs e)
    {
    	//Для того, что бы проинициализировать св-во, но не смешивать с другими активити.
      _obj.NumberVersion = -10;
    }
  }

}