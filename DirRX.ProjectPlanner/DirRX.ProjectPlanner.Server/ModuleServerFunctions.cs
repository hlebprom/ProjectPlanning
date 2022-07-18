using System;
using System.Collections.Generic;
using System.Linq;
using DirRX.Planner.Serialization;
using Sungero.Core;
using Sungero.CoreEntities;

namespace DirRX.ProjectPlanner.Server
{
	public class ModuleFunctions
	{
		[Remote]
		public static List<IOpensProjectPlansFromCard> GetOpensProjectPlansFromCard(IProjectPlan pp, DirRX.ProjectPlanning.IProject linkedProject)
		{
			return OpensProjectPlansFromCards.GetAll(x => ProjectPlans.Equals(x.PrjectPlan, pp) || DirRX.ProjectPlanning.Projects.Equals(x.Project, linkedProject)).ToList();
		}
		
		[Remote]
		public static void DeleteProjectActiviesByNumberVersion(int projectPlanId, int numVersion)
		{
			var prActs = ProjectActivities.GetAll(x => x.NumberVersion.Value == numVersion && x.ProjectPlan.Id == projectPlanId).ToList();
			
			foreach (var act in prActs)
			{
				if (act.Predecessors != null)
					act.Predecessors.Clear();
				act.LeadingActivity = null;
				act.Save();
			}
			
			foreach (var act in prActs)
			{
				ProjectActivities.Delete(act);
			}
		}
		
		[Remote]
		public static void DeleteProjectActivity(IProjectPlan projectPlan)
		{
			var linkedProject = Functions.ProjectPlan.GetLinkedProejct(projectPlan);
			
			if (!DirRX.ProjectPlanning.ProjectDocuments.GetAll(x => x.Project.Id == (linkedProject != null ? linkedProject.Id : 0)).Any() || linkedProject == null)
			{
				var prActs = ProjectActivities.GetAll(x => x.ProjectPlan.Id == projectPlan.Id).ToList();
				
				foreach (var act in prActs)
				{
					if (act.Predecessors != null)
						act.Predecessors.Clear();
					act.LeadingActivity = null;
					act.Save();
				}
				
				foreach (var act in prActs)
				{
					ProjectActivities.Delete(act);
				}
			}
		}
		
		[Remote]
		public static void CreateCopyVersion(IProjectPlan projectPlan, int sourceNumVersion)
		{
			var newVersion = projectPlan.Versions.AddNew();
			newVersion.AssociatedApplication = Sungero.Content.AssociatedApplications.GetAll(x => x.Extension == "rxpp").First();
			projectPlan.Save();
			
			var model = string.Empty;
			
			using (var reader = new System.IO.StreamReader(projectPlan.Versions.First(x => x.Number.Value == sourceNumVersion).Body.Read()))
			{
				model = reader.ReadToEnd();
			}
			
			SaveModelFromModelString(model, projectPlan, projectPlan.LastVersion.Number.Value, true, false);
		}
		
		[Remote]
		public static void CreateCopyProject(IProjectPlan projectPlan, int numVersion)
		{
			var model = string.Empty;
			
			using (var reader = new System.IO.StreamReader(projectPlan.Versions.First(x => x.Number.Value == numVersion).Body.Read()))
			{
				model = reader.ReadToEnd();
			}
			
			SaveModelFromModelString(model, projectPlan, projectPlan.LastVersion.Number.Value, false, true);
		}
		
		/// <summary>
		/// Записать тело Json-модели в свойство проекта "ModelBody".
		/// </summary>
		/// <param name="projectId">Ид проекта.</param>
		[Public, Remote]
		public static void WriteJsonBodyToProjectVersion(IProjectPlan projectPlan, int numVersion)
		{
			if (projectPlan != null)
			{
				var json = GetModel(projectPlan.Id, numVersion);
				
				using (var stream = new System.IO.MemoryStream())
				{
					var bytes = System.Text.Encoding.GetEncoding("UTF-8").GetBytes(json);
					stream.Write(bytes, 0, bytes.Length);
					
					if (numVersion == 0)
					{
						if (projectPlan.HasVersions)
						{
							var lastVersion = projectPlan.LastVersion;
							lastVersion.Body.Write(stream);
						}
						else
							projectPlan.CreateVersionFrom(stream, "rxpp");
					}
					else
					{
						var version = projectPlan.Versions.FirstOrDefault(x => x.Number.Value == numVersion);
						if (version != null)
							version.Body.Write(stream);
					}
				}
				projectPlan.IsCopy = false;
				projectPlan.Save();
			}
		}
		
		/// <summary>
		/// Сохранить модель из ссылки сервиса хранилищ.
		/// </summary>
		/// <param name="uriModel">Ссылка на файл.</param>
		[Remote(IsPure = true), Public]
		public static void SaveModelFromStorageByBinaryBodyId(string binaryBodyId, int projectPlanId, int version)
		{
			//Получаем Тип
			var dependency = Type.GetType("Sungero.WebAPI.Services.StorageServiceUrlBuilder, Sungero.WebAPI");

			//Получаем метод "GetDefaultStorageDownloadUrl"
			var getDefaultStorageDownloadUrlMethod = dependency.GetMethod("GetDefaultStorageDownloadUrl", System.Reflection.BindingFlags.Static | System.Reflection.BindingFlags.Public);

			//Получаем ссылку.
			Guid binaryDataGuid = Guid.Parse(binaryBodyId);
			var url = (Uri)getDefaultStorageDownloadUrlMethod.Invoke(null, new object[] { binaryDataGuid, "Model.json" });
			var modelJson = GetJsonStringByUri(url.AbsoluteUri);
			
			SaveModelFromGanttService(modelJson, projectPlanId, version);
		}
		
		[Public, Remote]
		public static string GetJsonStringByUri(string uriModel)
		{
			var client = new System.Net.WebClient();
			var bytesModel = client.DownloadData(uriModel);
			var stringModel = System.Text.Encoding.UTF8.GetString(bytesModel);
			
			return stringModel;
		}
		
		[Public, Remote]
		public static List<string> SaveModelFromModelString(string modelJson, IProjectPlan projectPlan, int numVersion, bool isCopyVersion, bool isCopyProject)
		{
			if (!isCopyVersion)
				DeleteProjectActiviesByNumberVersion(projectPlan.Id, numVersion);
			
			var model = modelJson.UnpackFromJson<DirRX.Planner.Model.Model>();
			
			var leadActivities = new List<Structures.Module.LeadActivities>();
			var notFoundEmployeesList = new List<string>();
			var idActivities = new List<Structures.Module.IDActivities>();
			
			foreach (var activityApp in model.Activities)
			{
				var activity = ProjectPlanner.ProjectActivities.Create();
				
				if (activityApp.LeadActivityId.HasValue)
				{
					leadActivities.Add(Structures.Module.LeadActivities.Create(activity, activityApp.LeadActivityId.Value));
				}
				
				idActivities.Add(Structures.Module.IDActivities.Create(activityApp.Id, activity.Id));
				activity.ProjectPlan = projectPlan;
				activity.Name = activityApp.Name;
				activity.Number = activityApp.CurrentNumber;
				activity.StartDate = activityApp.StartDate;
				activity.EndDate = activityApp.EndDate;
				activity.Duration = ProjectPlanner.Functions.Module.GetWorkiningDaysInPeriod(activity.StartDate.Value, activity.EndDate.Value).ToString();
				activity.BaselineWork = activityApp.BaselineWork;
				activity.ExecutionPercent = activityApp.ExecutionPercent;
				activity.Note = activityApp.Note;
				activity.SortIndex = activityApp.SortIndex;
				activity.Priority = activityApp.Priority;
				activity.FactualCosts = activityApp.FactualCosts;
				activity.PlannedCosts = activityApp.PlannedCosts;
				activity.NumberVersion = numVersion;
				
				foreach (var ar in projectPlan.AccessRights.Current)
				{
					activity.AccessRights.Grant(ar.Recipient, ar.AccessRightsType);
				}
				
				foreach (var status in activity.StatusAllowedItems)
				{
					if (activityApp.Status != null)
					{
						if (status.ToString() == activityApp.Status.EnumValue)
							activity.Status = status;
					}
				}
				
				foreach (var type in activity.TypeActivityAllowedItems)
				{
					if (type.ToString() == activityApp.TypeActivity)
						activity.TypeActivity = type;
				}
				
				if (isCopyVersion || isCopyProject)
				{
					activity.ResponsibleEmployee = activityApp.PerformerId.HasValue ? Sungero.Company.Employees.Get(activityApp.PerformerId.Value) : null;
				}
				else
				{
					var performerApp = model.Performers.FirstOrDefault(x => x.Id == (activityApp.PerformerId.HasValue ? activityApp.PerformerId.Value : -1));
					var performerRX = Sungero.Company.Employees.Null;
					
					if (performerApp != null)
					{
						performerRX = Sungero.Company.Employees.GetAll(x => (x.Name == performerApp.Name || (x.Person != null ? x.Person.DisplayValue == performerApp.Name : false)) &&
						                                               x.JobTitle.Name == performerApp.JobTitle &&
						                                               x.Status.Value == (performerApp.Active ? Sungero.Company.Employee.Status.Active : Sungero.Company.Employee.Status.Closed)).FirstOrDefault();
						if (performerRX == null)
						{
							var message = string.Format(DirRX.ProjectPlanner.Resources.EmployeeNotFound, performerApp.Name, performerApp.Id);
							if (!notFoundEmployeesList.Any(x => x.Contains(message)))
								notFoundEmployeesList.Add(message);
						}
						else
						{
							if (!projectPlan.TeamMembers.Where(x => Recipients.Equals(x.Member, performerRX)).Any())
							{
								var newMember = projectPlan.TeamMembers.AddNew();
								newMember.Member = performerRX;
								newMember.Group = DirRX.ProjectPlanner.ProjectPlanTeamMembers.Group.Change;
							}
							
							var linkedProject = DirRX.ProjectPlanner.Functions.ProjectPlan.GetLinkedProejct(projectPlan);
							if (linkedProject != null)
							{
								if (!linkedProject.TeamMembers.Where(x => Recipients.Equals(x.Member, performerRX)).Any())
								{
									var newMember = linkedProject.TeamMembers.AddNew();
									newMember.Member = performerRX;
									newMember.Group = DirRX.ProjectPlanner.ProjectPlanTeamMembers.Group.Change;
								}
							}
						}
					}
					
					activity.ResponsibleEmployee = performerRX;
				}
				activity.Save();
			}
			
			foreach (var leadAct in leadActivities)
			{
				var leadActivityId = idActivities.First(a => a.IDApp == leadAct.LeadActivityId).ID;
				leadAct.Activity.LeadingActivity = ProjectPlanner.ProjectActivities.Get(leadActivityId);
				leadAct.Activity.Save();
			}
			
			foreach (var activityApp in model.Activities.Where(x => x.Predecessors != null))
			{
				var activity = ProjectPlanner.ProjectActivities.Get(idActivities.First(a => a.IDApp == activityApp.Id).ID);
				
				foreach (var itemAct in activityApp.Predecessors)
				{
					var predecessor = ProjectPlanner.ProjectActivities.Get(idActivities.First(a => a.IDApp == itemAct.Id).ID);
					var item  = activity.Predecessors.AddNew();
					item.Activity = predecessor;
					item.LinkType = itemAct.LinkType;
				}
				activity.Save();
			}
			
			WriteJsonBodyToProjectVersion(projectPlan, numVersion);
			
			return notFoundEmployeesList;
		}
		
		/// <summary>
		/// Сохраняет изменения по проекту и этапам.
		/// </summary>
		/// <param name="modelXML">Строка с данными модели.</param>
		[Remote, Public]
		public static void SaveModelFromGanttService(string modelJson, int projectPlanId, int numVersion)
		{
			Logger.Debug(modelJson);
			
			var model = modelJson.UnpackFromJson<DirRX.Planner.Model.Model>();
			// Найти проект и обновить.
			var projectApp = model.Project;
			var project = ProjectPlans.Get(projectPlanId);
			
			if (!project.AccessRights.CanUpdate())
			{
				throw new Sungero.Domain.Shared.Exceptions.SecuritySystemException(false, Resources.ExceprionMessage);
			}
			
			project.Name = projectApp.Name;
			project.StartDate = projectApp.StartDate;
			project.EndDate = projectApp.EndDate;
			project.BaselineWork = projectApp.BaselineWork;
			project.ExecutionPercent = projectApp.ExecutionPercent;
			project.Note = projectApp.Note;
			
			var linkedProject = DirRX.ProjectPlanning.Projects.GetAll(x => ProjectPlans.Equals(project, x.ProjectPlanDirRX)).FirstOrDefault();
			
			if (linkedProject != null)
			{
				linkedProject.StartDate = project.StartDate;
				linkedProject.EndDate = project.EndDate;
				linkedProject.BaselineWork = project.BaselineWork;
				linkedProject.ExecutionPercent = project.ExecutionPercent;
				linkedProject.Note = project.Note;
			}
			
			project.Save();
			
			var lastId = model.LastActivityId;
			
			// Удалить этапы.
			var actListId = ProjectPlanner.ProjectActivities.GetAll(p => ProjectPlanning.Projects.Equals(project, p.ProjectPlan) && p.NumberVersion.Value == numVersion).Select(i => i.Id).ToList();
			var actAppListId = model.Activities.Where(i => i.Id <= lastId).Select(i => i.Id).ToList();
			var actRemoveList = actListId.Where(a => !actAppListId.Contains(a)).ToList();
			
			// TODO Нужно рефакторить -- многократные поиски подчиненных этапов.
			int curIndex = 0;
			
			while (actRemoveList.Any())
			{
				int curId = actRemoveList[curIndex];
				var act = ProjectActivities.Get(curId);
				if (!Functions.ProjectActivity.GetChildActivities(act).Any())
				{
					// Удаляемый этап является предшественником для любого другого этапа.
					foreach (var item in Functions.ProjectActivity.GetActivities(project).Where(x => x.Predecessors.Any(y => y.Activity.Equals(act))))
						item.Predecessors.Remove(item.Predecessors.First(x => x.Activity.Equals(act)));
					
					ProjectPlanner.ProjectActivities.Delete(act);
					actRemoveList.RemoveAt(curIndex);
				}
				else
					curIndex++;
				
				if (curIndex == actRemoveList.Count)
					curIndex = 0;
				
				project.PlannedCosts -= act.PlannedCosts;
				project.FactualCosts -= act.FactualCosts;
			}
			
			foreach (var actRemoveID in actRemoveList)
			{
				var prAct = ProjectPlanner.ProjectActivities.Get(actRemoveID);
				ProjectPlanner.ProjectActivities.Delete(prAct);
			}
			
			var idActivities = new List<Structures.Module.IDActivities>();
			var leadActivities = new List<Structures.Module.LeadActivities>();
			
			int? totalExPersent = 0;
			// Обновить или создать этапы.
			foreach (var activityApp in model.Activities)
			{
				DirRX.ProjectPlanner.IProjectActivity activity;
				
				// Создать новый этап.
				if (activityApp.Id > lastId)
				{
					activity = ProjectPlanner.ProjectActivities.Create();
					activity.ProjectPlan = project;
					idActivities.Add(Structures.Module.IDActivities.Create(activityApp.Id, activity.Id));
				}
				else
					activity = ProjectPlanner.ProjectActivities.Get(activityApp.Id);
				
				activity.Name = activityApp.Name;
				activity.Number = activityApp.CurrentNumber;
				activity.StartDate = activityApp.StartDate;
				activity.EndDate = activityApp.EndDate;
				// Длительность в рабочих днях.
				activity.Duration = ProjectPlanner.Functions.Module.GetWorkiningDaysInPeriod(activity.StartDate.Value, activity.EndDate.Value).ToString();
				activity.BaselineWork = activityApp.BaselineWork;
				activity.ExecutionPercent = activityApp.ExecutionPercent;
				activity.Note = activityApp.Note;
				activity.SortIndex = activityApp.SortIndex;
				activity.Priority = activityApp.Priority;
				activity.FactualCosts = activityApp.FactualCosts;
				activity.PlannedCosts = activityApp.PlannedCosts;
				activity.NumberVersion = numVersion;
				totalExPersent += activity.ExecutionPercent;
				
				foreach (var ar in project.AccessRights.Current)
				{
					activity.AccessRights.Grant(ar.Recipient, ar.AccessRightsType);
				}
				
				foreach (var status in activity.StatusAllowedItems)
				{
					if (activityApp.Status != null)
					{
						if (status.ToString() == activityApp.Status.EnumValue)
							activity.Status = status;
					}
				}
				
				foreach (var type in activity.TypeActivityAllowedItems)
				{
					if (type.ToString() == activityApp.TypeActivity)
						activity.TypeActivity = type;
				}
				
				
				// Подобрать ответственного по ID.
				// TODO надо кешировать ответственных.
				activity.ResponsibleEmployee = activityApp.PerformerId.HasValue ? Sungero.Company.Employees.Get(activityApp.PerformerId.Value) : null;
				
				// Подобрать ведущий этап.
				if (activityApp.LeadActivityId.HasValue)
				{
					if (activityApp.LeadActivityId.Value <= lastId)
					{
						activity.LeadingActivity = ProjectPlanner.ProjectActivities.Get(activityApp.LeadActivityId.Value);
					}
					else
						leadActivities.Add(Structures.Module.LeadActivities.Create(activity, activityApp.LeadActivityId.Value));
				}
				else
				{
					// Очистить ведущий этап.
					if (activity.LeadingActivity != null)
						activity.LeadingActivity = null;
				}
				
				activity.Save();
			}
			
			project.ExecutionPercent = totalExPersent / (model.Activities.Count > 0 ? model.Activities.Count : 1);
			
			// Дозаполнить ведущие этапы.
			foreach (var actStructure in leadActivities)
			{
				var leadActivityId = idActivities.FirstOrDefault(a => a.IDApp == actStructure.LeadActivityId).ID;
				actStructure.Activity.LeadingActivity = ProjectPlanner.ProjectActivities.Get(leadActivityId);
				actStructure.Activity.Save();
			}
			
			// Обновить предшественников для этапов.
			foreach (var activityApp in model.Activities)
			{
				// Выбрать текущий этап в БД.
				var activity = ProjectPlanner.ProjectActivities.Get(activityApp.Id > lastId ? idActivities.FirstOrDefault(a => a.IDApp == activityApp.Id).ID : activityApp.Id);
				if (activityApp.Predecessors != null)
				{
					activity.Predecessors.Clear();
					foreach (var itemAct in activityApp.Predecessors)
					{
						var predecessor = ProjectPlanner.ProjectActivities.Get(itemAct.Id > lastId ? idActivities.FirstOrDefault(a => a.IDApp == itemAct.Id).ID : itemAct.Id);
						var item  = activity.Predecessors.AddNew();
						item.Activity = predecessor;
						item.LinkType = itemAct.LinkType;
					}
					activity.Save();
				}
			}
			
			WriteJsonBodyToProjectVersion(project, numVersion);
		}
		
		
		/// <summary>
		/// Передает данные по проекту в модель Json.
		/// </summary>
		/// <param name="projectId">ИД проекта.</param>
		/// <returns>Сериализованная строка с данными по проекту.</returns>
		[Remote, Public]
		public static string GetModel(int projectPlanId, int numberVersion)
		{
			var model = new DirRX.Planner.Model.Model();
			var project = ProjectPlans.Get(projectPlanId);
			var projActs = ProjectActivities.GetAll(a => ProjectPlans.Equals(a.ProjectPlan, project) && a.NumberVersion.Value == numberVersion).ToList();
			
			AccessRights.AllowRead(() =>
			                       {
			                       	var projectApp = new DirRX.Planner.Model.Project();
			                       	projectApp.Id = projectPlanId;
			                       	projectApp.Name = project.Name;
			                       	projectApp.Stage = Sungero.Projects.Projects.Info.Properties.Stage.GetLocalizedValue(project.Stage);
			                       	projectApp.StartDate = project.StartDate;
			                       	projectApp.EndDate = project.EndDate;
			                       	projectApp.BaselineWork = project.BaselineWork;
			                       	projectApp.ExecutionPercent = project.ExecutionPercent;
			                       	projectApp.FactualCosts = project.FactualCosts;
			                       	projectApp.PlannedCosts = project.PlannedCosts;
			                       	
			                       	var linkedProject = DirRX.ProjectPlanning.Projects.GetAll(x => ProjectPlans.Equals(project, x.ProjectPlanDirRX)).FirstOrDefault();
			                       	
			                       	if (linkedProject != null)
			                       	{
			                       		projectApp.ManagerId = linkedProject.Manager.Id;
			                       		projectApp.Note = linkedProject.Note;
			                       		projectApp.UseBaseLineWorkInHours = linkedProject.BaselineWorkType != DirRX.ProjectPlanning.Project.BaselineWorkType.Money;

			                       	}
			                       	
			                       	var activitiesApp = new List<DirRX.Planner.Model.Activity>();
			                       	
			                       	foreach (var a in projActs)
			                       	{
			                       		var item = new DirRX.Planner.Model.Activity();
			                       		item.BaselineWork = a.BaselineWork;
			                       		item.LeadActivityId = a.LeadingActivity != null ? a.LeadingActivity.Id : (int?)null;
			                       		item.Id = a.Id;
			                       		item.StartDate = a.StartDate;
			                       		item.EndDate = a.EndDate;
			                       		item.Name = a.Name;
			                       		item.ExecutionPercent = a.ExecutionPercent;
			                       		item.Note = a.Note;
			                       		item.SortIndex = a.SortIndex.HasValue ? a.SortIndex.Value : 1;
			                       		item.PerformerId = a.ResponsibleEmployee != null ? a.ResponsibleEmployee.Id : (int?)null;
			                       		item.Predecessors = a.Predecessors.Where(x => x.Activity != null).Select(x => new DirRX.Planner.Model.Predecessor {Id = x.Activity.Id, LinkType = x.LinkType }).ToList();

			                       		var submittedTasks = Sungero.Workflow.Server.ModuleFunctions.GetTasksWhichContainEntityInAttachment(a)
			                       			.Where(t => t.Status != Sungero.Workflow.Task.Status.Aborted && t.Status != Sungero.Workflow.Task.Status.Suspended &&
			                       			       t.Status != Sungero.Workflow.Task.Status.Draft);
			                       		item.SubmittedTasks = submittedTasks.Select(t => new DirRX.Planner.Model.Task {Id = t.Id, DisplayValue = t.DisplayValue, HyperLink = Hyperlinks.Get(t)}).ToList<DirRX.Planner.Model.Task>();
			                       		item.UnfinishedTasks = submittedTasks.Where(t => t.Status != Sungero.Workflow.Task.Status.Completed)
			                       			.Select(t => new DirRX.Planner.Model.Task {Id = t.Id, DisplayValue = t.DisplayValue, HyperLink = Hyperlinks.Get(t), Deadline = t.MaxDeadline}).ToList<DirRX.Planner.Model.Task>();
			                       		item.Priority = a.Priority;
			                       		item.PlannedCosts = a.PlannedCosts;
			                       		item.FactualCosts = a.FactualCosts;
			                       		item.TypeActivity = a.TypeActivity.ToString();
			                       		item.Status = new DirRX.Planner.Model.ActivityStatus {EnumValue = a.Status.ToString(), LocalizeValue = a.Info.Properties.Status.GetLocalizedValue(a.Status)};
			                       		activitiesApp.Add(item);
			                       	}
			                       	
			                       	var employees = new List<Sungero.Company.IEmployee>();
			                       	if (linkedProject != null)
			                       		employees.Add(linkedProject.Manager);
			                       	foreach (var recepient in project.TeamMembers.Select(p => p.Member))
			                       	{
			                       		var employee = Sungero.Company.Employees.As(recepient);
			                       		if (employee != null)
			                       		{
			                       			employees.Add(employee);
			                       			continue;
			                       		}
			                       		
			                       		var group = Sungero.CoreEntities.Groups.As(recepient);
			                       		if (group != null)
			                       		{
			                       			var inGroup = Sungero.CoreEntities.Groups.GetAllUsersInGroup(group).Select(x => Sungero.Company.Employees.As(x)).Where(x => x != null);
			                       			employees.AddRange(inGroup);
			                       		}
			                       	}
			                       	
			                       	var performersApp = new List<DirRX.Planner.Model.Performer>();
			                       	foreach (var employee in employees.Distinct())
			                       	{
			                       		var performer = new DirRX.Planner.Model.Performer();
			                       		performer.Active = employee.Status == Sungero.CoreEntities.DatabookEntry.Status.Active;
			                       		performer.Id = employee.Id;
			                       		performer.JobTitle = employee.JobTitle != null ? employee.JobTitle.Name : string.Empty;
			                       		performer.Name = employee.Person != null ? employee.Person.DisplayValue : employee.Name;
			                       		performersApp.Add(performer);
			                       	}
			                       	
			                       	var workingTimeCalendarApp = new DirRX.Planner.Model.WorkingTimeCalendar();
			                       	var days = new List<DateTime>();
			                       	foreach (var year in Sungero.CoreEntities.WorkingTime.GetAll().Where(c => c.Status == Sungero.CoreEntities.DatabookEntry.Status.Active &&
			                       	                                                                     c.Year >= Calendar.Now.Year))
			                       	{
			                       		days.AddRange(year.Day.Where(d => d.Kind.HasValue).Select(d => d.Day));
			                       	}
			                       	workingTimeCalendarApp.FreeDays = days;
			                       	
			                       	var action = new Enumeration("Create");
			                       	var idS = projActs.Select(x => x.Id).ToList<int>();
			                       	var history = Histories.GetAll(x => x.Action.Value == action && idS.Contains(x.EntityId.Value)).OrderByDescending(x => x.HistoryDate.Value).FirstOrDefault();
			                       	
			                       	var prAct = ProjectActivities.Create();
			                       	var listStatuses = new List<DirRX.Planner.Model.ActivityStatus>();
			                       	foreach (Enumeration i in prAct.StatusAllowedItems)
			                       	{
			                       		listStatuses.Add(new DirRX.Planner.Model.ActivityStatus {EnumValue = i.ToString(), LocalizeValue = prAct.Info.Properties.Status.GetLocalizedValue(i)});
			                       	}
			                       	
			                       	model.ActivityStatuses = listStatuses;
			                       	model.LastActivityId = history != null ? history.EntityId.Value : 0;
			                       	model.Project = projectApp;
			                       	model.Activities = activitiesApp.ToList();
			                       	model.Performers = performersApp.ToList();
			                       	model.WorkingTimeCalendar = workingTimeCalendarApp;
			                       });
			
			var modelJson = DirRX.Planner.Serialization.Serializable.PackToJson(model);
			
			return modelJson;
		}
		
		
		/// <summary>
		///  Создает задачи по выбранным этапам.
		/// </summary>
		/// <param name="activityIds">Список ид этапов.</param>
		/// <returns>Список задач по этапам.</returns>
		[Remote(IsPure = true), Public]
		public List<Sungero.Workflow.ISimpleTask> CreateTasks(List<int> activityIds)
		{
			var result = new List<Sungero.Workflow.ISimpleTask>();

			foreach (var activityId in activityIds)
			{
				var activity = ProjectPlanner.ProjectActivities.Get(activityId);
				var subject = string.Format(Resources.SEND_TASK_SUBJECT, activity.Name);
				var newTask = Sungero.Workflow.SimpleTasks.Create(subject);
				
				if (activity.ResponsibleEmployee != null)
				{
					var step = newTask.RouteSteps.AddNew();
					step.Performer = activity.ResponsibleEmployee;
				}
				
				newTask.Deadline = activity.EndDate.Value;
				newTask.ActiveText = Resources.SEND_TASK_ACTIVE_TEXT;
				newTask.Attachments.Add(activity);
				newTask.Attachments.Add(activity.ProjectPlan);
				newTask.Save();
				result.Add(newTask);
			}
			return result;
		}
		
		[Remote, Public]
		public static Sungero.Workflow.ISimpleTask CreateTask(int activityId)
		{
			var activity = ProjectPlanner.ProjectActivities.Get(activityId);
			var subject = string.Format(Resources.SEND_TASK_SUBJECT, activity.Name);
			var newTask = Sungero.Workflow.SimpleTasks.Create(subject);
			
			if (activity.ResponsibleEmployee != null)
			{
				var step = newTask.RouteSteps.AddNew();
				step.Performer = activity.ResponsibleEmployee;
			}
			
			newTask.Deadline = activity.EndDate.Value;
			newTask.ActiveText = Resources.SEND_TASK_ACTIVE_TEXT;
			newTask.Attachments.Add(activity);
			newTask.Attachments.Add(activity.ProjectPlan);
			newTask.Save();
			
			return newTask;
			
		}
		
		/// <summary>
		/// Возвращает список задач по их идентификаторам.
		/// </summary>
		/// <param name="tasksIds">Список ИД задач.</param>
		/// <returns>Список задач.</returns>
		[Remote(IsPure = true), Public]
		public static IQueryable<Sungero.Workflow.ISimpleTask> GetTasksByIds(List<int> tasksIds)
		{
			return Sungero.Workflow.SimpleTasks.GetAll().Where(c => tasksIds.Contains(c.Id));
		}
		
		[Remote(IsPure = true)]
		public static bool VersionApproved(Sungero.Content.IElectronicDocument project, int numVersion)
		{
			return Signatures.Get(project.Versions.First(x => x.Number.Value == numVersion)).Where(s => s.SignatureType == SignatureType.Approval).Any();
		}
		
		[Remote, Public]
		public static string GetWebSite()
		{
			var configSettingsName = System.IO.Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "_ConfigSettings.xml");
			var xd = new System.Xml.XmlDocument();

			try
			{
				xd.Load(configSettingsName);
				var nodes = xd.DocumentElement.SelectNodes(Constants.Module.XmlNodeTenant);
				if  (nodes.Count > 0)
				{
					var res = string.Empty;
					var nodesEnum = nodes.GetEnumerator();
					while (nodesEnum.MoveNext())
					{
						var currentNode = (System.Xml.XmlNode)nodesEnum.Current;
						if (currentNode.Attributes["name"].Value == TenantInfo.TenantId)
							res = currentNode.Attributes["gantt_site"].Value;
					}
					
					nodesEnum.Reset();
					
					return res;
				}
				else
				{
					var node = xd.DocumentElement.SelectSingleNode(Constants.Module.XmlNodeSite);
					return node.Attributes["value"].Value;
				}
			}
			catch (Exception ex)
			{
				Logger.ErrorFormat("Ошибка при чтении конфигурационного файла: {0}", ex.Message);
			}
			
			return string.Empty;
		}
		
		
		/// <summary>
		/// Проверить наличие у участника прав на сущность.
		/// </summary>
		/// <param name="entity">Сущность.</param>
		/// <param name="member">Участник.</param>
		/// <param name="accessRightsType">Тип прав.</param>
		/// <returns>True - если права есть, иначе - false.</returns>
		public static bool CheckGrantedRights(Sungero.Domain.Shared.IEntity entity, IRecipient member, Guid accessRightsType)
		{
			if (accessRightsType == DefaultAccessRightsTypes.Change)
				return entity.AccessRights.IsGrantedDirectly(accessRightsType, member) ||
					entity.AccessRights.IsGrantedDirectly(DefaultAccessRightsTypes.FullAccess, member);
			
			if (accessRightsType == Constants.Module.ChangeContent)
				return entity.AccessRights.IsGrantedDirectly(accessRightsType, member) ||
					entity.AccessRights.IsGrantedDirectly(DefaultAccessRightsTypes.Change, member) ||
					entity.AccessRights.IsGrantedDirectly(DefaultAccessRightsTypes.FullAccess, member);
			
			if (accessRightsType == DefaultAccessRightsTypes.Read)
				return entity.AccessRights.IsGrantedDirectly(accessRightsType, member) ||
					entity.AccessRights.IsGrantedDirectly(DefaultAccessRightsTypes.FullAccess, member) ||
					entity.AccessRights.IsGrantedDirectly(DefaultAccessRightsTypes.Change, member) ||
					entity.AccessRights.IsGrantedDirectly(Constants.Module.ChangeContent, member);
			
			return entity.AccessRights.IsGrantedDirectly(accessRightsType, member);
		}
		
	}
}