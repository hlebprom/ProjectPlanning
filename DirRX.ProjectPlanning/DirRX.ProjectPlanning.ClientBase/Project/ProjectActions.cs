using System;
using System.Collections.Generic;
using System.Linq;
using Sungero.Core;
using Sungero.CoreEntities;
using DirRX.ProjectPlanning.Project;
using DirRX.ProjectPlanning;

namespace DirRX.ProjectPlanning.Client
{
	partial class ProjectActions
	{

		public virtual void PlanProject(Sungero.Domain.Client.ExecuteActionArgs e)
		{
			if (_obj.ProjectPlanDirRX == null)
			{
				var dialog = Dialogs.CreateTaskDialog(DirRX.ProjectPlanning.Projects.Resources.NotFoundProjectPlan, MessageType.Question);
				dialog.Buttons.AddYesNo();
				
				if (dialog.Show() == DialogButtons.Yes)
				{
					var plan = DirRX.ProjectPlanner.ProjectPlans.Create();
					plan.Name = string.Format(DirRX.ProjectPlanning.Projects.Resources.TitleProjectPlan, _obj.ShortName);
					plan.Stage = _obj.Stage;
					plan.StartDate = _obj.StartDate;
					plan.EndDate = _obj.EndDate;
					plan.ActualStartDate = _obj.ActualStartDate;
					plan.ActualFinishDate = _obj.ActualFinishDate;
					plan.ExecutionPercent = _obj.ExecutionPercent;
					plan.BaselineWork = _obj.BaselineWork;
					plan.BaselineWorkType = _obj.BaselineWorkType;
					plan.PlannedCosts = _obj.PlannedCosts;
					plan.FactualCosts = _obj.FactualCosts;
					
					foreach (var member in _obj.TeamMembers)
					{
						var planMember = plan.TeamMembers.AddNew();
						planMember.Member = member.Member;
						planMember.Group = member.Group;
					}
					
					plan.Show();
				}
			}
			else
			{
				if (!_obj.ProjectPlanDirRX.HasVersions)
				{
					_obj.ProjectPlanDirRX.Show();
				}
				else
				{
					var webSite = DirRX.ProjectPlanner.PublicFunctions.Module.Remote.GetWebSite();
					
					try
					{
						if (!string.IsNullOrEmpty(webSite))
						{
							var lockMessage = DirRX.ProjectPlanner.PublicFunctions.ProjectPlan.SetLockOnProjectPlan(_obj.ProjectPlanDirRX);
							if (string.IsNullOrEmpty(lockMessage))
								DirRX.ProjectPlanner.PublicFunctions.Module.RunPlannerApp(webSite, _obj.ProjectPlanDirRX.Id, _obj.ProjectPlanDirRX.LastVersion.Number.Value, Users.Current.Id, false);
							else
							{
								var dialog = Dialogs.CreateTaskDialog(string.Format(DirRX.ProjectPlanner.ProjectPlans.Resources.ProjectPlanIsLocked,lockMessage, Environment.NewLine), MessageType.Question);
								dialog.Buttons.AddYesNo();
								
								if (dialog.Show() == DialogButtons.Yes)
									DirRX.ProjectPlanner.PublicFunctions.Module.RunPlannerApp(webSite, _obj.ProjectPlanDirRX.Id, _obj.ProjectPlanDirRX.LastVersion.Number.Value, Users.Current.Id, true);
							}
						}
					}
					catch (Exception ex)
					{
						Dialogs.ShowMessage(DirRX.ProjectPlanning.Projects.Resources.ErrorMessage, MessageType.Error);
						Logger.DebugFormat("{0} {1}", ex.Message, ex.InnerException != null ? ex.InnerException.Message : string.Empty);
					}
				}
			}
		}

		public virtual bool CanPlanProject(Sungero.Domain.Client.CanExecuteActionArgs e)
		{
			return !_obj.State.IsChanged;
		}
	}

}