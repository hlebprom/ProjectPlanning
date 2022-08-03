using System;
using System.Collections.Generic;
using System.Linq;
using Sungero.Core;
using Sungero.CoreEntities;
using DirRX.ProjectPlanner.ProjectActivity;

namespace DirRX.ProjectPlanner.Client
{

	partial class ProjectActivityActions
	{

    public override void SetAccessRights(Sungero.Domain.Client.ExecuteActionArgs e)
    {
      e.Params.AddOrUpdate("NeedOpenWeb", false);
      base.SetAccessRights(e);
    }

    public override bool CanSetAccessRights(Sungero.Domain.Client.CanExecuteActionArgs e)
    {
      return base.CanSetAccessRights(e);
    }

		public virtual void ShowActivities(Sungero.Domain.Client.ExecuteActionArgs e)
		{
			var activities = Functions.ProjectActivity.Remote.GetChildActivities(_obj);
			activities.Show();
		}

		public virtual bool CanShowActivities(Sungero.Domain.Client.CanExecuteActionArgs e)
		{
			return true;
		}

		public virtual void SendToPerformers(Sungero.Domain.Client.ExecuteActionArgs e)
		{
			
		}

		public virtual bool CanSendToPerformers(Sungero.Domain.Client.CanExecuteActionArgs e)
		{
			return true;
		}

	}

}