using System;
using System.Collections.Generic;
using System.Linq;
using Sungero.Core;
using Sungero.CoreEntities;
using DirRX.ProjectPlanner.ProjectActivity;

namespace DirRX.ProjectPlanner
{
	partial class ProjectActivityClientHandlers
	{

		public override void Showing(Sungero.Presentation.FormShowingEventArgs e)
		{
			var webSite = DirRX.ProjectPlanner.PublicFunctions.Module.Remote.GetWebSite();
			DirRX.ProjectPlanner.PublicFunctions.Module.RunPlannerApp(webSite, _obj.ProjectPlan.Id, _obj.Id, _obj.NumberVersion.Value, Users.Current.Id, true);
		}

		public virtual void NumberValueInput(Sungero.Presentation.IntegerValueInputEventArgs e)
		{
			if (e.NewValue.HasValue && e.NewValue.Value < 1)
				e.AddError(ProjectActivities.Resources.NumberMustBePositive);
		}

		public virtual void ExecutionPercentValueInput(Sungero.Presentation.IntegerValueInputEventArgs e)
		{
			if (e.NewValue.HasValue && (e.NewValue.Value < 0 || e.NewValue.Value > 100))
				e.AddError(Sungero.Projects.Projects.Resources.IncorrectPercent);
		}

		public virtual void BaselineWorkValueInput(Sungero.Presentation.DoubleValueInputEventArgs e)
		{
			if (e.NewValue.HasValue && e.NewValue.Value < 0)
				e.AddError(DirRX.ProjectPlanning.Projects.Resources.IncorrectBaselineWork);
		}

	}
}