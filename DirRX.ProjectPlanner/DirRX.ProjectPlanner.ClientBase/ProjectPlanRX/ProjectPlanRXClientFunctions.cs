using System;
using System.Collections.Generic;
using System.Linq;
using DirRX.Planner.Model.Serialization;
using Sungero.Core;
using Sungero.CoreEntities;
using DirRX.ProjectPlanner.ProjectPlanObsolete;
using DirRX.Planner.Model;
using net.sf.mpxj;
using net.sf.mpxj.MpxjUtilities;
using net.sf.mpxj.reader;

namespace DirRX.ProjectPlanner.Client
{
  partial class ProjectPlanRXFunctions
  {
    /// <summary>
		/// Заблокировать план проекта перед его открытием.
		/// </summary>
		/// <param name="projectPlan">План проекта</param>
		/// <returns>Строка с сообщением о блокировке. Возвращается пустая строка, если блокировок нет.</returns>
		[Public]
		public static string SetLockOnProjectPlan(IProjectPlanRX projectPlan)
		{
			if (projectPlan.AccessRights.CanUpdate())
			{
				var lockinfoProjectPlan = Locks.GetLockInfo(projectPlan);
				var relatedProject = Functions.ProjectPlanRX.Remote.GetLinkedProject(projectPlan);
				
				try
				{
					if (relatedProject != null)
					{
						var lockInfoProject = Locks.GetLockInfo(relatedProject);
						if (lockInfoProject.IsLockedByOther)
							return lockInfoProject.LockedMessage.ToString(TenantInfo.Culture);
					}
					
					if (lockinfoProjectPlan.IsLockedByOther)
						return lockinfoProjectPlan.LockedMessage.ToString(TenantInfo.Culture);
					
					Locks.TryLock(projectPlan);
					
					if (relatedProject != null)
						Locks.TryLock(relatedProject);
					
					return string.Empty;
				}
				catch (Exception ex)
				{
					lockinfoProjectPlan = Locks.GetLockInfo(projectPlan);
					if (lockinfoProjectPlan.IsLockedByMe)
						Locks.Unlock(projectPlan);
					
					throw new Exception(string.Format("Ошибка при установке блокировки на проект или план проекта: {0}", ex.Message), ex);
				}
			}
			else
				return string.Empty;
		}
		
		public void CreateProjectFromFileDialog(int numVersion)
		{
			var dfile = Dialogs.CreateInputDialog(DirRX.ProjectPlanner.ProjectPlanRXes.Resources.SelectFileDialogTitle);
			var file = dfile.AddFileSelect(DirRX.ProjectPlanner.ProjectPlanRXes.Resources.ProjectFile, true);
			file.WithFilter(DirRX.ProjectPlanner.ProjectPlanRXes.Resources.ProjectFiles, "rxpp");
			file.WithFilter(DirRX.ProjectPlanner.ProjectPlanRXes.Resources.MsProject, "mpp");
			
			if (dfile.Show() == DialogButtons.Ok)
			{
				var model = string.Empty;
				
				if (string.IsNullOrEmpty(_obj.Name))
					_obj.Name = System.IO.Path.GetFileNameWithoutExtension(file.Value.Name);
				
				if ( System.IO.Path.GetExtension(file.Value.Name).ToLower() == ".mpp")
					model = Functions.ProjectPlanRX.GetModelFromMSProject(file);
				else if (System.IO.Path.GetExtension(file.Value.Name).ToLower() == ".rxpp")
					model = System.Text.Encoding.UTF8.GetString(file.Value.Content);
				else
				{
					Dialogs.ShowMessage(DirRX.ProjectPlanner.ProjectPlanRXes.Resources.ErrorSelectFile, MessageType.Error);
					return;
				}
				
				Functions.Module.Remote.SaveModelFromModelString(model, _obj, numVersion, false, false);
				
				var webSite = DirRX.ProjectPlanner.PublicFunctions.Module.Remote.GetWebSite();
				DirRX.ProjectPlanner.PublicFunctions.Module.RunPlannerApp(webSite, _obj.Id, _obj.LastVersion.Number.Value, Sungero.CoreEntities.Users.Current.Id, false);
			}
			else
			{
			  return;
			}
		}
		
		/// <summary>
		/// Создать модель проекта из проекта MS Project.
		/// </summary>
		/// <param name="file">Диалог выбора файла msProject</param>
		/// <returns>Модель</returns>
		[Public]
		public static string GetModelFromMSProject(CommonLibrary.IFileSelectDialogValue file)
		{
		  // Zheleznov_AV HACK в RX 4.1 сборку прикладной перевели на .NET Standart 2.0
		  // Используемая для конвертации mpp файлов библиотека mpjx не поддерживает .NET Standart.
		  // В короткое время не смогли найти решение этой проблемы. Времено отключаем функцию импорта планов из mpp файлов.
		  // В будущем надо будет найти нормальное решение.
			var model = new Model();
			var reader = new UniversalProjectReader();
			ProjectFile msProject = null;

			using (var stream = new java.io.ByteArrayInputStream(file.Value.Content))
			{
				msProject = reader.Read(stream);
			}

			foreach (net.sf.mpxj.Task task in msProject.Tasks)
			{
				if (task.ID.intValue() == 0)
					model.Project = CreateProject(task);
				else
				{
				  var activity = CreateActivity(task, msProject);
				  if (activity != null)
				    model.Activities.Add(activity);
				}
			}

			model.LastActivityId = -1;
			model.NumberVersion = 0;

			return DirRX.Planner.Model.Serialization.Serializable.PackToJson(model);
		}

		private static DirRX.Planner.Model.Activity CreateActivity(net.sf.mpxj.Task task, ProjectFile msProject)
		{
			var activity = new Activity();
			if (task.BaselineWork != null)
			  activity.BaselineWork = task.BaselineWork.Duration;
			activity.Name = task.Name;
			
			if (task.Name == null || task.Name == "" || task.Start == null || task.Finish == null)
			  return null;
			
			if (task.Start != null)
			 activity.StartDate = task.Start.ToDateTime();
			
			if (task.Finish != null)
			 activity.EndDate = task.Finish.ToDateTime().AddDays(1);
			
			if (task.PercentageComplete != null)
			 activity.ExecutionPercent = task.PercentageComplete.intValue();
			
			if (task.ActualCost != null)
			 activity.FactualCosts = task.ActualCost.intValue();
			
			activity.Id = task.ID.intValue();
			activity.Note = task.Notes;
		  activity.SortIndex = task.ID.intValue();
			activity.TypeActivity = GetTypeActivity(task);
			activity.LeadActivityId = GetLeadActivity(msProject, task);
			
			if (task.Predecessors != null)
			activity.Predecessors = task.Predecessors.ToIEnumerable<net.sf.mpxj.Relation>().Select(x => new Predecessor { LinkType = GetLinkType(x.Type.name()), Id = x.TargetTask.ID.intValue()}).ToList();
			
			if (task.Priority != null)
			activity.Priority = task.Priority.Value / 100;
			
			if (activity.ExecutionPercent != null)
			activity.Status = GetActivtyStatus(activity.ExecutionPercent.Value);
			
			return activity;
		}

		private static Project CreateProject(net.sf.mpxj.Task task)
		{
			var project = new Project();
			project.Name = task.Name;
			project.StartDate = task.Start.ToDateTime();
			project.EndDate = task.Finish.ToDateTime();
			project.BaselineWork = task.BaselineWork.Duration;
			project.ExecutionPercent = task.PercentageComplete.intValue();
			project.FactualCosts = task.ActualCost.intValue();
			project.UseBaseLineWorkInHours = true;
			project.Id = task.ID.intValue();
			project.Note = task.Notes;
			project.Stage = StageType.Initiation.ToString();

			return project;
		}

		private static int? GetLeadActivity(ProjectFile msProject, net.sf.mpxj.Task task)
		{
			int? result = null;

			foreach (net.sf.mpxj.Task t in msProject.Tasks)
			{
				if (t.ChildTasks.ToIEnumerable<net.sf.mpxj.Task>().Any(x => x.ID == task.ID) && t.ID.intValue() != 0)
				{
					result = t.ID.intValue();
					break;
				}
			}

			return result;
		}

		private static string GetTypeActivity(net.sf.mpxj.Task task)
		{
			var type = string.Empty;
			if (task.Milestone)
				type = DirRX.ProjectPlanner.ProjectActivity.TypeActivity.Milestone.ToString();
			else if (task.ChildTasks.ToIEnumerable<net.sf.mpxj.Task>().Any())
				type = DirRX.ProjectPlanner.ProjectActivity.TypeActivity.Section.ToString();
			else
				type = DirRX.ProjectPlanner.ProjectActivity.TypeActivity.Task.ToString();

			return type;
		}

		private static string GetLinkType(string msType)
		{
			var type = string.Empty;

			if (msType == "FINISH_START")
				type = "0";
			else if (msType == "START_START")
				type = "1";
			else if (msType == "FINISH_FINISH")
				type = "2";
			else if (msType == "START_FINISH")
				type = "3";
			return type;
		}

		private static DirRX.Planner.Model.ActivityStatus GetActivtyStatus(int persentComplite)
		{
			var status = new DirRX.Planner.Model.ActivityStatus();
			if (persentComplite == 100)
			{
				status.EnumValue = DirRX.ProjectPlanner.ProjectActivity.Status.Completed.ToString();
				status.LocalizeValue = DirRX.ProjectPlanner.ProjectActivities.Info.Properties.Status.GetLocalizedValue(DirRX.ProjectPlanner.ProjectActivity.Status.Completed);

			}
			else
			{
				status.EnumValue = DirRX.ProjectPlanner.ProjectActivity.Status.InWork.ToString();
				status.LocalizeValue = DirRX.ProjectPlanner.ProjectActivities.Info.Properties.Status.GetLocalizedValue(DirRX.ProjectPlanner.ProjectActivity.Status.InWork);
			}

			return status;
		}
  }
}