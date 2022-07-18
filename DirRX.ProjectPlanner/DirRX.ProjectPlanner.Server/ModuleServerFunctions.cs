using System;
using System.Collections.Generic;
using System.Linq;
using DirRX.Planner.Model.Serialization;
using Sungero.Core;
using Sungero.Company;
using Sungero.CoreEntities;
using ProjectPlannerModel;
using DirRX.ProjectPlanner;

namespace DirRX.ProjectPlanner.Server
{
  public class ModuleFunctions
  {
    /// <summary>
    /// Проверяет успешное прохождение инициализации.
    /// </summary>
    /// <returns>Ссылка на веб-клиент.</returns>
    /// <exception cref="Exception">Если проверка не пройдена.</exception>
    [Public(WebApiRequestType = RequestType.Get)]
    public string Check()
    {
      try
      {
        this.ThrowExceptionIfResourceLinksTableNotExist();
        
        return DirRX.ProjectPlanner.PublicFunctions.Module.Remote.GetWebSite();
      }
      catch (Exception ex)
      {
        throw new Exception(Resources.HealthCheckExceptionText, ex);
      }
    }
    
    private void ThrowExceptionIfResourceLinksTableNotExist()
    {
      using (var command = SQL.GetCurrentConnection().CreateCommand())
      {
        command.CommandText = Queries.Module.CheckResourceLinksTable;
        if ((int)command.ExecuteScalar() == 0)
        {
          throw new Exception(Resources.ResourceLinksTableNotFoundExceptionText);
        }
      }
    }

    /// <summary>
    /// Метод ищет получателей, в текущей реализации, только среди сотрудников.
    /// </summary>
    [Public(WebApiRequestType = RequestType.Get)]
    public Structures.Module.ISearchRecipientsResult SearchRecipients(string searchValue)
    {
      try
      {
        if (String.IsNullOrWhiteSpace(searchValue)) {
          return Structures.Module.SearchRecipientsResult.Create();
        }
        return this.HandleSearchRecipients(searchValue);
      }
      catch (Exception ex)
      {
        throw new Exception(DirRX.ProjectPlanner.Resources.SearchRecipientsExceptionFormat(searchValue), ex);
      }
    }

    /// <summary>
    /// 
    /// </summary>
    [Public(WebApiRequestType = RequestType.Post)]
    public static Structures.Module.IProjectPlanDto UpdatePlanData(int projectPlanId, int numberVersion)
    {
      try
      {
        var projectPlanDto = new Structures.Module.ProjectPlanDto();
        AccessRights.AllowRead(() =>
                               {
                                 projectPlanDto.ResourceData = HandleUpdateResourcesDataDto(projectPlanId, numberVersion);
                                 projectPlanDto.Tasks = GetRxTasks(projectPlanId, numberVersion);
                                 projectPlanDto.BaselineWorkType = GetPlanBaselineWorkType(projectPlanId);
                               });
        return projectPlanDto;
      }
      catch (Exception ex)
      {
        throw new Exception(DirRX.ProjectPlanner.Resources.UpdatePlanDataExceptionFormat(projectPlanId, numberVersion), ex);
      }
    }
    
    /// <summary>
    /// Проверить существует ли целевая версия в плане.
    /// </summary>
    /// <returns></returns>
    [Public(WebApiRequestType = RequestType.Get)]
    public Structures.Module.ICheckPlanExistsResult CheckPlanExists(int projectPlanId, int numberVersion)
    {
      try
      {
        var projectPlan = ProjectPlanRXes.GetAll(x => x.Id == projectPlanId).FirstOrDefault();
        var planVersionExist = projectPlan != null && projectPlan.Versions.Any(x => x.Number.HasValue ? x.Number.Value == numberVersion : false);
        return new Structures.Module.CheckPlanExistsResult {IsExists = planVersionExist};
      }
      catch (Exception ex)
      {
        throw new Exception(DirRX.ProjectPlanner.Resources.CheckPlanExistsExeptionFormat(projectPlanId, numberVersion), ex);
      }
    }

    /// <summary>
    /// 
    /// </summary>
    [Public(WebApiRequestType = RequestType.Post)]
    public Structures.Module.ICapacityResponseDto GetCapacityDto(List<int> resourceIds, double startDate, double endDate, int planId, int planVersion)
    {
      try
      {
        var startDateFromTimeStamp = new DateTime(1970, 1, 1, 0, 0, 0, 0).AddSeconds(startDate);
        var endDateFromTimeStamp = new DateTime(1970, 1, 1, 0, 0, 0, 0).AddSeconds(endDate);
        return new Structures.Module.CapacityResponseDto()
        {
          Capacity = GetCapacity(resourceIds, startDateFromTimeStamp, endDateFromTimeStamp, planId, planVersion),
          WorkingTimeCalendar = GetWorkingTimeCalendars(resourceIds, startDateFromTimeStamp, endDateFromTimeStamp)
        };
      }
      catch (Exception ex)
      {
        throw new Exception(DirRX.ProjectPlanner.Resources.GetCapacityExceptionFormat(resourceIds, startDate, endDate, ex));
      }
    }
    
    /// <summary>
    /// Найти или создать ресурс сотрудника.
    /// </summary>
    /// <param name="employeeId"></param>
    /// <returns></returns>
    [Public(WebApiRequestType = RequestType.Get)]
    public Structures.ProjectsResource.IProjectResourceDto GetOrCreateResource(int employeeId)
    {
      try
      {
        return this.HandleGetOrCreateResource(employeeId);
      }
      catch (Exception ex)
      {
        throw new Exception(DirRX.ProjectPlanner.Resources.ExecutionRequestExeptionFormat(ex, employeeId));
      }
    }

    private Structures.Module.ISearchRecipientsResult HandleSearchRecipients(string searchValue)
    {
      const int maxRecepintCount = 25;
      const int thumbnailDiameterInPx = 36;
      
      var recipients = Recipients.GetAll(r =>
                                         r.Status == Sungero.CoreEntities.DatabookEntry.Status.Active
                                         && !Sungero.CoreEntities.Shared.RolesRegistry.PredefinedRoles.Contains(r.Sid.Value)
                                         && r.Name.ToLower().Contains(searchValue.ToLower()));
      
      var recipientDtos = new List<Structures.Module.IRecipientDto>();
      foreach(var recipient in recipients)
      {
        //Zheleznov_AV в текущей реализации нам нужны только пользователи, исключая системных.
        var isSystemRecipient = recipient.IsSystem.HasValue ? recipient.IsSystem.Value : false;
        if (!Users.Is(recipient) || isSystemRecipient) {
          continue;
        }
        
        if (recipientDtos.Count >= maxRecepintCount) {
          break;
        }
        
        var recipientDto = new Structures.Module.RecipientDto
        {
          Id = recipient.Id,
          Name = recipient.Name
        };
        
        var thumbnail = Users.As(recipient).GetPersonalPhotoThumbnail(thumbnailDiameterInPx);
        if (thumbnail != null) {
          recipientDto.Thumbnail = Convert.ToBase64String(thumbnail);
        }
        
        recipientDtos.Add(recipientDto);
      }
      return Structures.Module.SearchRecipientsResult.Create(recipientDtos);
    }

    /// <summary>
    /// 
    /// </summary>
    private static void AddEmployeeResources(List<int> projectPlanResourceIds, List<Structures.Module.IUser> users, List<Structures.Module.IResource> resources)
    {
      var employeeResources = ProjectsResources.GetAll(x => x.Type.ServiceName == Constants.Module.ResourceTypes.Users && projectPlanResourceIds.Contains(x.Id));
      foreach (var employeeResource in employeeResources)
      {
        if (!users.Any(x => x.Id == employeeResource.Employee.Id))
        {
          users.Add(new Structures.Module.User()
                    {
                      Id = employeeResource.Employee.Id,
                      Name = employeeResource.Employee.Person != null ? employeeResource.Employee.Person.Name : employeeResource.Employee.Name,
                    });
        }
        resources.Add(new Structures.Module.Resource()
                      {
                        Id = employeeResource.Id,
                        EntityTypeId = employeeResource.Type.Id,
                        EntityId = employeeResource.Employee.Id,
                        UnitLabel = employeeResource.Type.MeasureUnit
                      });
      }
    }
    
    /// <summary>
    /// 
    /// </summary>
    private static void FillUsersFromResponsibles(List<DirRX.ProjectPlanner.IProjectActivity> activities, List<Structures.Module.IUser> users)
    {
      foreach(var activity in activities.Where(a => a.Responsible != null))
      {
        if (!users.Any(x => x.Id == activity.Responsible.Id))
        {
          users.Add(new Structures.Module.User
                    {
                      Id = activity.Responsible.Id,
                      Name = activity.Responsible.Person.Name
                    });
        }
      }
    }
    
    /// <summary>
    /// 
    /// </summary>
    private static List<Structures.Module.IMaterialResource> GetMaterialResources()
    {
      //HACK В данной версии материальные рксурсы не используются.
      return new List<Structures.Module.IMaterialResource>();
    }
    
    /// <summary>
    /// 
    /// </summary>
    /// Может ли быть пустой???
    private static List<Structures.Module.IResourceTypes> GetResourcesTypes()
    {
      var resourceTypes = new List<Structures.Module.IResourceTypes>();
      foreach(var type in ProjectResourceTypes.GetAll())
      {
        resourceTypes.Add(new Structures.Module.ResourceTypes()
                          {
                            Id = type.Id,
                            Name = type.Name,
                            SectionName = type.ServiceName
                          });
      }
      return resourceTypes;
    }
    
    /// <summary>
    /// 
    /// </summary>
    private static Structures.Module.IUser TryGetProjectManager(DirRX.ProjectPlanner.IProjectPlanRX project)
    {
      var linkedProject = DirRX.ProjectPlanning.Projects.GetAll(x => ProjectPlanRXes.Equals(project, x.ProjectPlanDirRX)).FirstOrDefault();
      if (linkedProject != null && linkedProject.Manager != null)
      {
        return new Structures.Module.User()
        {
          Id = linkedProject.Manager.Id,
          Name = linkedProject.Manager.Person.Name
        };
      }
      return null;
    }
    
    private Structures.ProjectsResource.IProjectResourceDto HandleGetOrCreateResource(int employeeId)
    {
      //Если ресурс есть, то просто возвращаем его
      var resourceByEmployeeId = DirRX.ProjectPlanner.ProjectsResources.GetAll(x => x.Employee != null && x.Employee.Id == employeeId).FirstOrDefault();
      if (resourceByEmployeeId != null)
      {
        return new Structures.ProjectsResource.ProjectResourceDto()
        {
          ResourceId = resourceByEmployeeId.Id,
          ResourceName = resourceByEmployeeId.Name
        };
      }
      //Если ресурса нет, то создаем новый
      //получаем сотрудника
      var employee = Employees.GetAll(x => x.Id == employeeId).FirstOrDefault();
      if (employee == null){
        throw new Exception(DirRX.ProjectPlanner.Resources.EmployeeIdNotFoundExeptionFormat(employeeId));
      }
      //получаем тип ресурса
      var userResourceType = DirRX.ProjectPlanner.ProjectResourceTypes.GetAll(x => x.ServiceName == Constants.Module.ResourceTypes.Users).FirstOrDefault();
      if (userResourceType == null)
      {
        throw new Exception(DirRX.ProjectPlanner.Resources.NullReferenceResourceTypeExceptionFormat(Constants.Module.ResourceTypes.Users));
      }
      //Создаем ресурс
      var newResource = DirRX.ProjectPlanner.ProjectsResources.Create();
      newResource.Type = userResourceType;
      newResource.Employee = employee;
      newResource.Name = newResource.Employee.Name;
      newResource.Save();
      //Возвращаем новый ресурс
      return new Structures.ProjectsResource.ProjectResourceDto()
      {
        ResourceId = newResource.Id,
        ResourceName = newResource.Name
      };
    }
    
    private static List<int> GetPlanOldTasksQuery(int activityId)
    {
      var oldTaskIds = new List<int>();
      using (var command = SQL.GetCurrentConnection().CreateCommand())
      {
        try
        {
          Logger.Debug("Execute get activity old tasks query");
          command.CommandText = string.Format(Queries.Module.GetPlanOldTasks, activityId);
          using (var reader = command.ExecuteReader())
          {
            while (reader.Read())
            {
              try 
              {
                oldTaskIds.Add((int)reader[0]);
              }
              catch (Exception ex) 
              {
                Logger.ErrorFormat("Не найдена задача с ID = {0}, Exception: {1} Trace: {2}", reader[0], ex.ToString(), ex.StackTrace.ToString());
              }
            }
          }
        }
        catch (Exception ex) 
        {
          Logger.ErrorFormat("Ошибка выполнения запроса. Exception: {0}, Trace: {1}", ex.ToString(), ex.StackTrace.ToString());
        }
      }
      return oldTaskIds;
    }
    
    private static List<Structures.Module.ITask> GetOldActivityTasks(int activityId)
    {
      var result = new List<Structures.Module.ITask>();
      var activity = ProjectPlanner.ProjectActivities.Get(activityId);
      var oldTaskIds = GetPlanOldTasksQuery(activityId);
      
      foreach (var taskId in oldTaskIds)
      {
        try
        {
          var task = Sungero.Workflow.SimpleTasks.Get(taskId);
        
          if (!task.MaxDeadline.HasValue)
          {
            Logger.Error(string.Format("у RxTask с id={0} не задан обязательный параметр MaxDeadline", task.Id));
          }
          if (task.Status == Sungero.Workflow.Task.Status.Aborted)
          {
            continue;
          }
  
          var taskStatus = "Unfinished";
          if (task.Status == Sungero.Workflow.Task.Status.Completed)
          {
            taskStatus = "Submitted";
          }
          result.Add(new Structures.Module.Task()
                     {
                       ActivityId = activity.Id,
                       Deadline = (task.MaxDeadline.HasValue ? task.MaxDeadline.Value : Calendar.Now),
                       HyperLink = Hyperlinks.Get(task),
                       Id = task.Id,
                       TaskStatus = taskStatus,
                       DisplayValue = task.DisplayValue
                     });
        }
        catch (Exception ex)
        {
          Logger.ErrorFormat("Произошла ошибка при преобразовании oldTask с ID = {0} Exception: {1}. Ttace: {2}", taskId, ex.ToString(), ex.StackTrace.ToString());
        }
      }
        
      
      return result;
    }
    
    /// <summary>
    /// 
    /// </summary>
    private static List<Structures.Module.ITask> GetRxTasks(int projectPlanId, int numVersion)
    {
      var rxTasks = new List<Structures.Module.ITask>();
      
      foreach (var activity in ProjectActivities.GetAll(x => x.NumberVersion.Value == numVersion && x.ProjectPlan.Id == projectPlanId))
      {
        var tasks = ProjectActivityTasks.GetAll(x => x.ProjectActivity.Id == activity.Id);
        foreach (var task in tasks)
        {
          if (!task.MaxDeadline.HasValue)
          {
            throw new Exception(DirRX.ProjectPlanner.Resources.TaskMaxDeadLineExeptionFormat(task.Id));
          }
          // Zheleznov_AV HACK клиент сейчас не умеет обрабатывать отмененные задачи.
          // Надо синхронизировать модель статусов на киленте и на сервере.
          if (task.Status == Sungero.Workflow.Task.Status.Aborted)
          {
            continue;
          }
          var taskStatus = "Unfinished";
          if (task.Status == Sungero.Workflow.Task.Status.Completed)
          {
            taskStatus = "Submitted";
          }
          rxTasks.Add(new Structures.Module.Task()
                      {
                        ActivityId = activity.Id,
                        Deadline = task.MaxDeadline.Value,
                        HyperLink = Hyperlinks.Get(task),
                        Id = task.Id,
                        TaskStatus = taskStatus,
                        DisplayValue = task.DisplayValue
                      });
        }
        rxTasks.AddRange(GetOldActivityTasks(activity.Id));
      }
      return rxTasks;
    }
    
    /// <summary>
    /// Получение открытых из карточки планов.
    /// </summary>
    [Remote]
    public static List<IOpensProjectPlansFromCard> GetOpensProjectPlansFromCard(IProjectPlanRX pp, DirRX.ProjectPlanning.IProject linkedProject)
    {
      return OpensProjectPlansFromCards.GetAll(x => ProjectPlanRXes.Equals(x.PrjectPlan, pp) || (linkedProject != null && DirRX.ProjectPlanning.Projects.Equals(x.Project, linkedProject))).ToList();
    }
    
    /// <summary>
    /// Удаление всех этапов для указанной версии плана.
    /// </summary>
    /// <param name="projectPlanId">Id плана проекта</param>
    /// <param name="numVersion">Номер версии плана</param>
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
    
    /// <summary>
    /// Удаление всех этапов плана проекта.
    /// </summary>
    [Remote]
    public static void DeleteProjectActivity(IProjectPlanRX projectPlan)
    {
      var linkedProject = Functions.ProjectPlanRX.GetLinkedProject(projectPlan);
      
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
    
    /// <summary>
    /// Создание копии версии плана проекта.
    /// </summary>
    /// <param name="projectPlan">План проекта</param>
    /// <param name="sourceNumVersion">Номер исходной версии</param>
    [Remote]
    public static void CreateCopyVersion(IProjectPlanRX projectPlan, int sourceNumVersion)
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
    
    /// <summary>
    /// Создание копии проекта
    /// </summary>
    [Remote]
    public static void CreateCopyProject(IProjectPlanRX projectPlan, int numVersion)
    {
      var model = string.Empty;
      
      using (var reader = new System.IO.StreamReader(projectPlan.Versions.First(x => x.Number.Value == numVersion).Body.Read()))
      {
        model = reader.ReadToEnd();
      }
      
      SaveModelFromModelString(model, projectPlan, projectPlan.LastVersion.Number.Value, false, true);
    }
    
    [Public(WebApiRequestType = RequestType.Post), Remote]
    public static void WriteJsonBodyToProjectVersion(int projectPlanId, int numVersion, bool writeToPublicBody)
    {
      var projectPlan = ProjectPlanRXes.Get(projectPlanId);
      WriteJsonBodyToProjectVersion(projectPlan, numVersion, writeToPublicBody);
    }
    
    /// <summary>
    /// Записать тело Json-модели в свойство проекта "ModelBody".
    /// </summary>
    /// <param name="projectPlan">Ссылка на план проекта.</param>
    /// <param name="numVersion">Порядковый номер версии.</param>
    /// <param name="writeToPublicBody">Записать в Read-only тело.</param>
    [Public, Remote]
    public static void WriteJsonBodyToProjectVersion(IProjectPlanRX projectPlan, int numVersion, bool writeToPublicBody)
    {
      if (projectPlan != null)
      {
        try
        {
          string jsonBody = GetModel(projectPlan, numVersion);
          
          using (var stream = new System.IO.MemoryStream())
          {
            var bytes = System.Text.Encoding.GetEncoding("UTF-8").GetBytes(jsonBody);
            stream.Write(bytes, 0, bytes.Length);
            
            if (numVersion == 0)
            {
              if (projectPlan.HasVersions)
              {
                var lastVersion = projectPlan.LastVersion;
                if (writeToPublicBody)
                {
                  lastVersion.PublicBody.Write(stream);
                }
                else
                {
                  lastVersion.Body.Write(stream);
                }
              }
              else
              {
                projectPlan.CreateVersionFrom(stream, "rxpp");
              }
              
            }
            else
            {
              var version = projectPlan.Versions.FirstOrDefault(x => x.Number.Value == numVersion);
              if (version != null)
              {
                if (writeToPublicBody)
                {
                  version.PublicBody.Write(stream);
                }
                else
                {
                  version.Body.Write(stream);
                }
              }
              ///HACK Urmanov_AR: Фикс ошибки с ресурсами при создании плана из файла.
              else
              {
                projectPlan.CreateVersionFrom(stream, "rxpp");
              }
            }
            projectPlan.IsCopy = false;
            projectPlan.Save();
            //HACK несмотря на using в коробке принято явно закрывать стрим
            stream.Close();
          }
          
        }
        catch(Exception ex)
        {
          Logger.DebugFormat("При сохранении плана проекта ID {0} произошла ошибка. {1}", projectPlan.Id, ex.Message+ex.StackTrace);
        }
      }
    }
    
    /// <summary>
    /// Запросить модель из сервиса хранилищ по ссылке.
    /// </summary>
    /// <param name="uriModel">Ссылка</param>
    [Public, Remote]
    public static string GetJsonStringByUri(string uriModel)
    {
      var client = new System.Net.WebClient();
      var bytesModel = client.DownloadData(uriModel);
      var stringModel = System.Text.Encoding.UTF8.GetString(bytesModel);
      
      return stringModel;
    }
    
    /// <summary>
    /// Создаёт активити из модели.
    /// </summary>
    /// <param name="activityModel">Модель активити.</param>
    /// <param name="projectPlan">План проекта.</param>
    /// <param name="numVersion">Номер версии.</param>
    /// <returns></returns>
    private static ProjectPlanner.IProjectActivity CreateActivityFromModelString(DirRX.Planner.Model.Activity activityModel, IProjectPlanRX projectPlan, Nullable<int> numVersion)
    {
      var newActivity = ProjectPlanner.ProjectActivities.Create();

      newActivity.Name = activityModel.Name;
      if (string.IsNullOrWhiteSpace(newActivity.Name))
      {
        return null;
      }
      
      newActivity.Number = activityModel.CurrentNumber;
      newActivity.StartDate = activityModel.StartDate;
      newActivity.EndDate = activityModel.EndDate;
      if (!newActivity.StartDate.HasValue || !newActivity.EndDate.HasValue)
      {
        return null;
      }
      newActivity.ProjectPlan = projectPlan;
      
      newActivity.Duration = ProjectPlanner.Functions.Module.GetWorkiningDaysInPeriod(newActivity.StartDate.Value, newActivity.EndDate.Value).ToString();
      newActivity.BaselineWork = activityModel.BaselineWork;
      newActivity.ExecutionPercent = activityModel.ExecutionPercent;
      newActivity.Note = activityModel.Note;
      newActivity.SortIndex = activityModel.SortIndex;
      newActivity.Priority = activityModel.Priority;
      newActivity.FactualCosts = activityModel.FactualCosts;
      newActivity.PlannedCosts = activityModel.PlannedCosts;
      newActivity.NumberVersion = numVersion;
      
          
      foreach (var ar in projectPlan.AccessRights.Current)
      {
        newActivity.AccessRights.Grant(ar.Recipient, ar.AccessRightsType);
      }
      
      if (activityModel.Status != null)
      {
        var status = newActivity.StatusAllowedItems.Where(s => activityModel.Status.EnumValue == s.Value).FirstOrDefault();
          
        if (status != null)
        {
          newActivity.Status = status;
        }
      }
      
      var type = newActivity.TypeActivityAllowedItems.Where(t => t != null && t.Value == activityModel.TypeActivity).FirstOrDefault();
      
      if (type != null)
      {
        newActivity.TypeActivity = type;
      }
      
      if (activityModel.ResponsibleId.HasValue)
      {
        var responsible = Sungero.Company.Employees.GetAll(x => x.Id == activityModel.ResponsibleId.Value).FirstOrDefault();
      
        if (responsible == null)
        {
          Logger.Debug(DirRX.ProjectPlanner.Resources.EmployeeForActivityNotFoundFormat(activityModel.ResponsibleId.Value, activityModel.Name));
        }
        else
        {
          newActivity.Responsible = responsible;
        
          if (!projectPlan.TeamMembers.Any(x => Recipients.Equals(x.Member, newActivity.Responsible)))
          {
            var newMember = projectPlan.TeamMembers.AddNew();
            newMember.Member = newActivity.Responsible;
            newMember.Group = DirRX.ProjectPlanner.ProjectPlanRXTeamMembers.Group.Change;
          }
          
          var linkedProject = DirRX.ProjectPlanner.Functions.ProjectPlanRX.GetLinkedProject(projectPlan);
          if (linkedProject != null)
          {
            if (!linkedProject.TeamMembers.Any(x => x.Member.Id == newActivity.Responsible.Id))
            {
              var newMember = linkedProject.TeamMembers.AddNew();
              newMember.Member = newActivity.Responsible;
              newMember.Group = DirRX.ProjectPlanner.ProjectPlanRXTeamMembers.Group.Change;
            }
          }
        }
      }
      
      return newActivity;
    }
    
    public static void UpdatePredeccessorsFromModel(DirRX.Planner.Model.Model model, List<Structures.Module.ModelActivity> modelActivitiesList)
    {
      foreach (var activityApp in model.Activities.Where(x => x.Predecessors != null))
      {
        var oldActivityModel = modelActivitiesList.FirstOrDefault(a => a.ModelActivitylId == activityApp.Id);
        if (oldActivityModel == null)
        {
          continue;
        }
        
        var activity = ProjectPlanner.ProjectActivities.Get(oldActivityModel.ID);
        foreach (var itemAct in activityApp.Predecessors)
        {
          var idActivityPredec = modelActivitiesList.FirstOrDefault(a => a.ModelActivitylId == itemAct.Id);
          if (idActivityPredec == null)
          {
            continue;
          }
          
          var predecessor = ProjectPlanner.ProjectActivities.Get(idActivityPredec.ID);
          var item  = activity.Predecessors.AddNew();
          item.Activity = predecessor;
          item.LinkType = itemAct.LinkType;
        }
        activity.Save();
      }
    }
    
    private static void SetLeadActivities(List<Structures.Module.ModelActivity> modelActivitiesList, List<Structures.Module.LeadActivities> leadActivities)
    {
      foreach (var leadActivityStruct in leadActivities)
      {
        var leadActivity = modelActivitiesList.FirstOrDefault(a => a.ModelActivitylId == leadActivityStruct.LeadActivityId);
        if (leadActivity == null)
        {
          continue;
        }
        
        leadActivityStruct.Activity.LeadingActivity = ProjectPlanner.ProjectActivities.Get(leadActivity.ID);
        leadActivityStruct.Activity.Save();
      }
    }
    
    /// <summary>
    /// Разбор json модели плана проекта.
    /// </summary>
    [Public, Remote]
    public static List<string> SaveModelFromModelString(string modelJson, IProjectPlanRX projectPlan, int numVersion, bool isCopyVersion, bool isCopyProject)
    {
      using (var connection = CreateDBConnection())
      {
        if (!isCopyVersion)
          DeleteProjectActiviesByNumberVersion(projectPlan.Id, numVersion);
        
        var model = DirRX.Planner.Model.Serialization.Serializable.UnpackFromJson<DirRX.Planner.Model.Model>(modelJson);
        
        var leadActivities = new List<Structures.Module.LeadActivities>();
        var modelActivitiesList = new List<Structures.Module.ModelActivity>();
        var notFoundEmployeeIds = new List<string>();
        
        foreach (var activityModel in model.Activities)
        {
          var newActivity = CreateActivityFromModelString(activityModel, projectPlan, numVersion);

          if (newActivity == null)
          {
            continue;
          }
          modelActivitiesList.Add(Structures.Module.ModelActivity.Create(activityModel.Id.Value, newActivity.Id));
          
          if (activityModel.LeadActivityId.HasValue)
          {
            leadActivities.Add(Structures.Module.LeadActivities.Create(newActivity, activityModel.LeadActivityId.Value));
          }
          
          RefreshCapacityInfo(activityModel.Resources, newActivity);
          
          newActivity.Save();
          
          if (isCopyVersion)
          {
            RemoveActivityResources(activityModel.Id.Value, connection);
          }
          else
          {
            RemoveActivityResources(newActivity.Id, connection);
          }
          
          SaveResources(activityModel.Resources, newActivity.Id, activityModel.StartDate.Value, activityModel.EndDate.Value, connection);
        }
        
        UpdatePredeccessorsFromModel(model, modelActivitiesList);
        
        SetLeadActivities(modelActivitiesList, leadActivities);
        
        WriteJsonBodyToProjectVersion(projectPlan, numVersion, false);
        
        return notFoundEmployeeIds;
      }
    }
    
    private static void InsertResourceLink(int activityId, int resourceId, double busy, System.Data.IDbConnection connection)
    {
      using (var command = connection.CreateCommand())
        {
          command.CommandText = "insert into ResourceLinks values(@project_activity_id, @resource_id, @average_busy)";
          SQL.AddParameter(command, "@project_activity_id", activityId, System.Data.DbType.Int32);
          SQL.AddParameter(command, "@resource_id", resourceId, System.Data.DbType.Int32);
          SQL.AddParameter(command, "@average_busy", busy, System.Data.DbType.Double);
          command.ExecuteScalar();
        }
    }
    
    /// <summary>
    /// Сохранение изменений в ресурсах проекта.
    /// </summary>
    public static void SaveResources(List<Planner.Model.ResourcesWorkload> resourcesWorkload, int activityId, DateTime startDate, DateTime endDate, System.Data.IDbConnection connection)
    {
      foreach(var resourceWorkload in resourcesWorkload)
      {
        var resource = ProjectsResources.GetAll(x => x.Id == resourceWorkload.ResourceId).SingleOrDefault();
        if (resource == null)
        {
          Logger.Debug(DirRX.ProjectPlanner.Resources.ResourceNotFoundFormat(resourceWorkload.ResourceId));
          continue;
        }
        
        //TODO Urmanov: вот тут была важная проверка на то что ресурсов не найдено, надо сделать с новыми ресурсами
        var activityLengthInDays = WorkingTime.GetDurationInWorkingDays(startDate, endDate, resource) - 1;
        var busy = resourceWorkload.Value;
        
        if (activityLengthInDays > 1)
          busy = resourceWorkload.Value / activityLengthInDays;
        
        InsertResourceLink(activityId, resourceWorkload.ResourceId, busy, connection);
      }
    }
    
    private static System.Data.IDbConnection CreateDBConnection()
    {
      System.Data.IDbConnection connection;
      
      connection = SQL.CreateConnection();
      
      if (connection.State != System.Data.ConnectionState.Open)
      {
        connection.Open();
      }
      
      return connection;
    }
    
    
    /// <summary>
    /// Удаление упоминаний активити в ресурсах.
    /// </summary>
    public static void RemoveActivityResources(int activityId, System.Data.IDbConnection connection)
    {
      using (var command = connection.CreateCommand())
      {
        command.CommandText = "delete from ResourceLinks where project_activity_id = @project_activity_id";
        SQL.AddParameter(command, "@project_activity_id", activityId, System.Data.DbType.Int32);
        command.ExecuteScalar();
      }
      
    }
    
    /// <summary>
    /// Обновление информации о трудоемкости.
    /// </summary>
    public static void RefreshCapacityInfo(List<DirRX.Planner.Model.ResourcesWorkload> resources, IProjectActivity activity)
    {
      var deletingCapasities = new List<IProjectActivityResourcesCapacity>();
      foreach(var capacity in activity.ResourcesCapacity)
      {
        if(!resources.Any(x => x.ResourceId == capacity.ResourceId))
        {
          deletingCapasities.Add(capacity);
        }
      }
      
      foreach(var deletingCapacity in deletingCapasities)
        activity.ResourcesCapacity.Remove(deletingCapacity);
      
      foreach(var resourceCapacity in resources)
      {
        IProjectActivityResourcesCapacity capacity = new ProjectActivityResourcesCapacity();
        capacity = activity.ResourcesCapacity.Where(x => x.ResourceId == resourceCapacity.ResourceId).FirstOrDefault();
        if (capacity == null)
        {
          capacity = activity.ResourcesCapacity.AddNew();
        }
        capacity.ResourceId = resourceCapacity.ResourceId;
        capacity.Capacity = resourceCapacity.Value;
      }
    }
    
    /// <summary>
    /// Сохраняет изменения по проекту и этапам.
    /// </summary>
    /// <param name="model">Dto - модель плана проекта.</param>
    [Remote, Public(WebApiRequestType = RequestType.Post)]
    public void SaveModelFromGanttService(DirRX.ProjectPlanner.Structures.Module.IGanttProjectPlanDto model)
    {
      //найти проект и обновить.
      var projectApp = model.Project;
      var project = ProjectPlanRXes.Get(model.ProjectPlanId);

      var projectLockInfo = Locks.GetLockInfo(project);
      if (projectLockInfo.IsLockedByOther)
      {
        Logger.DebugFormat("Не удалось сохранить проект. Проект заблокирован пользователем {0}.", projectLockInfo.OwnerName);
        return;
      }

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
      
      
      var linkedProject = DirRX.ProjectPlanning.Projects.GetAll(x => ProjectPlanRXes.Equals(project, x.ProjectPlanDirRX)).FirstOrDefault();
      
      // Zheleznov_AV TODO уточнить во время рефакторинга, надо ли менять linkedProject. Если надо, то что именно менять, а что нет.
      // Тут есть странное поведение, linkedProject вроде меняется, но у него метод Save не вызывается. Возможно это ошибка.
      if (linkedProject != null)
      {
        linkedProject.StartDate = project.StartDate;
        linkedProject.EndDate = project.EndDate;
        linkedProject.BaselineWork = project.BaselineWork;
        linkedProject.ExecutionPercent = project.ExecutionPercent;

        if (projectApp.ManagerId.HasValue)
        {
          linkedProject.Manager = Sungero.Company.Employees.Get(projectApp.ManagerId.Value);
        }
      }
      
      var lastId = model.LastActivityId;
      
      // Удалить этапы.
      var actListId = ProjectPlanner.ProjectActivities.GetAll(p => ProjectPlanning.Projects.Equals(project, p.ProjectPlan) && p.NumberVersion.Value == model.NumberVersion).Select(i => i.Id).ToList();
      var actAppListId = model.Activities.Where(i => i.Id <= lastId).Select(i => i.Id).ToList();
      var actRemoveList = actListId.Where(a => !actAppListId.Contains(a)).ToList();
      
      // TODO Нужно рефакторить -- многократные поиски подчиненных этапов.
      int curIndex = 0;
      
      while (actRemoveList.Count > 0)
      {
        int curId = actRemoveList[curIndex];
        var act = ProjectActivities.Get(curId);
        if (!Functions.ProjectActivity.GetChildActivities(act).Any())
        {
          // Удаляемый этап является предшественником для любого другого этапа.
          foreach (var item in Functions.ProjectActivity.GetActivities(project).Where(x => x.Predecessors.Any(y => y.Activity.Equals(act))))
          {
            item.Predecessors.Remove(item.Predecessors.First(x => x.Activity.Equals(act)));
          }
          try
          {
            var tasks = ProjectActivityTasks.GetAll(t => t.ProjectActivity.Id == act.Id);
            
            foreach (var task in tasks)
            {
              task.Abort();
              task.ProjectActivity = null;
              task.Save();
            }
            
            var notifications = RXTaskNotices.GetAll(t => t.ProjectActivity.Id == act.Id);
            
            foreach (var notify in notifications)
            {
              notify.ProjectActivity = null;
              notify.Save();
            }
            
            var assignments = Assignments.GetAll(t => t.ProjectActivity.Id == act.Id);
            
            foreach (var assignment in assignments)
            {
              assignment.Abort();
              assignment.ProjectActivity = null;
              assignment.Save();
            }

            ProjectPlanner.ProjectActivities.Delete(act);
            actRemoveList.RemoveAt(curIndex);
          }
          catch(Exception ex)
          {
            var errorMessage = string.Format("Невозможно удалить активити с id {0}, Error: {1}", act.Id, ex.Message + Environment.NewLine + ex.StackTrace);
            Logger.Error(errorMessage);
            throw new Exception(errorMessage);
          }
        }
        else
          curIndex++;
        
        if (curIndex == actRemoveList.Count)
          curIndex = 0;
      }
     
      
      var updatedActivityIds = new List<Structures.Module.ModelActivity>();
      var leadActivities = new List<Structures.Module.LeadActivities>();
      
      //Zheleznov_AV надо обновить все этапы. Операция обновления одного этапа стоит дорого. Поэтому будем сохранять все этапы за раз.
      var activitiesForBatchSaving = new List<DirRX.ProjectPlanner.IProjectActivity>(model.Activities.Count);
      
      int? totalExPersent = 0;
      using (var connection = CreateDBConnection())
      {
        // Обновить или создать этапы.
        foreach (var activityApp in model.Activities)
        {
          DirRX.ProjectPlanner.IProjectActivity activity;
          
          // Создать новый этап.
          if (activityApp.Id > lastId)
          {
            activity = ProjectPlanner.ProjectActivities.Create();
            activity.ProjectPlan = project;
            
            var idActivity = Structures.Module.ModelActivity.Create(activityApp.Id, activity.Id);
            
            updatedActivityIds.Add(idActivity);
            activityApp.Id = idActivity.ID;
          }
          else
          {
            activity = ProjectPlanner.ProjectActivities.Get(activityApp.Id);
          }
          
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
          activity.NumberVersion = model.NumberVersion;
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
          
          
          if (activityApp.ResponsibleId.HasValue)
          {
            activity.Responsible = Sungero.Company.Employees.Get(activityApp.ResponsibleId.Value);
          }
          else
          {
            activity.Responsible = null;
          }
          
          // Подобрать ведущий этап.
          if (activityApp.LeadActivityId.HasValue)
          {
            if (activityApp.LeadActivityId.Value <= lastId)
            {
              activity.LeadingActivity = ProjectPlanner.ProjectActivities.Get(activityApp.LeadActivityId.Value);
            }
            else
            {
              var leadActivity = Structures.Module.LeadActivities.Create(activity, activityApp.LeadActivityId.Value);
              leadActivities.Add(leadActivity);
            }
          }
          else
          {
            // Очистить ведущий этап.
            activity.LeadingActivity = null;
          }
          
          RefreshCapacityInfo(activityApp.Resources.Select(r => new DirRX.Planner.Model.ResourcesWorkload() {ResourceId = r.ResourceId, Value = r.Value} ).ToList(), activity);
          
          activitiesForBatchSaving.Add(activity);
          
          RemoveActivityResources(activity.Id, connection);
          SaveResources(
            activityApp.Resources.Select(r => 
                                         new Planner.Model.ResourcesWorkload()
                                         {
                                           ResourceId = r.ResourceId,
                                           Value = r.Value
                                         }).ToList(),
            activity.Id,
            activityApp.StartDate,
            activityApp.EndDate.Value,
            connection);
        }
      }
      
      BatchSave(activitiesForBatchSaving);
      activitiesForBatchSaving.Clear();
      
      project.ExecutionPercent = totalExPersent / (model.Activities.Count > 0 ? model.Activities.Count : 1);
      
      
      // Дозаполнить ведущие этапы.
      foreach (var actStructure in leadActivities)
      {
        var updatedIdItem = updatedActivityIds.FirstOrDefault(a => a.ModelActivitylId == actStructure.LeadActivityId);
        if (updatedIdItem == null)
        {
          throw new Exception(string.Format("не удалось найти обновленный id для активити с id={0}", actStructure.LeadActivityId));
        }
        actStructure.Activity.LeadingActivity = ProjectPlanner.ProjectActivities.Get(updatedIdItem.ID);
        activitiesForBatchSaving.Add(actStructure.Activity);
      }
      BatchSave(activitiesForBatchSaving);
      activitiesForBatchSaving.Clear();
      
      
      // Обновить предшественников для этапов и ведущие этапы для сохраняемой в Storage модели.
      foreach (var activityApp in model.Activities)
      {
        if (activityApp.Predecessors != null && activityApp.Predecessors.Count > 0)
        {
          var activity = ProjectPlanner.ProjectActivities.Get(activityApp.Id);
          activity.Predecessors.Clear();
          foreach (var itemAct in activityApp.Predecessors)
          {
            int predecessorId = itemAct.Id.Value;
            if (itemAct.Id.Value > lastId)
            {
              var updatedPredeccessorId = updatedActivityIds.FirstOrDefault(a => a.ModelActivitylId == itemAct.Id);
              if (updatedPredeccessorId == null)
              {
                throw new Exception(string.Format("не удалось найти обновленный id для активити с id={0}", itemAct.Id));
              }
              predecessorId = updatedPredeccessorId.ID;
            }
            
            var predecessor = ProjectPlanner.ProjectActivities.Get(predecessorId);
            var item  = activity.Predecessors.AddNew();
            item.Activity = predecessor;
            item.LinkType = itemAct.LinkType;
            itemAct.Id = predecessor.Id;
          }
          activitiesForBatchSaving.Add(activity);
        }
        
        if (activityApp.LeadActivityId.HasValue && activityApp.LeadActivityId.Value > lastId)
        {
          var updatedIdItem = updatedActivityIds.FirstOrDefault(a => a.ModelActivitylId == activityApp.LeadActivityId.Value);
          if (updatedIdItem == null)
          {
            throw new Exception(string.Format("не удалось найти обновленный id для активити с id={0}", activityApp.LeadActivityId.Value));
          }
          activityApp.LeadActivityId = new Nullable<int>(updatedIdItem.ID);
        }
      }
      BatchSave(activitiesForBatchSaving);
      activitiesForBatchSaving.Clear();
      
      project.PlannedCosts = projectApp.PlannedCosts;
      project.FactualCosts = projectApp.FactualCosts;
      project.Save();
      WriteJsonBodyToProjectVersion(project, model.NumberVersion, false);
    }
    
    /// <summary>
    /// Массовое сохранение сущностей в отдельной сессии БД.
    /// </summary>
    /// <param name="activities">Сущности для сохранения.</param>
    private static void BatchSave(IEnumerable<object> entities)
    {
      if (!entities.Any())
        return;
      
      //Zheleznov_AV HACK используем не совсем легальное API для массового сохранения. Причина - долгое сохранение отдельных сущностей.
      //Габбасов Руслан планировал отправить пожелание в платформу для предоставления легального API массового сохранениея.
      using (var session = Sungero.Domain.Session.CreateIndependentSession()){
        foreach(var entity in entities){
          session.Update(entity);
        }
        session.SubmitChanges();
      }
    }
    
    private static List<int> GetProjectResources(int projectPlanId, int numberVersion)
    {
      using (var connection = CreateDBConnection())
      {
        var resourcesList = new List<int>();
         
        using (var command = connection.CreateCommand())
        {
          //получить ресурсы по активити
          command.CommandText = "select distinct resource_id from ResourceLinks rl "+
                                "join DirRX_Projec1_PrjctActivity activities on rl.project_activity_id = activities.id " +
                                "where (activities.ProjectPlan = @projectPlanId AND activities.NumberVersion = @VersionNum)";
          SQL.AddParameter(command, "@projectPlanId", projectPlanId, System.Data.DbType.Int32);
          SQL.AddParameter(command, "@VersionNum", numberVersion, System.Data.DbType.Int32);
          using (var reader = command.ExecuteReader())
          while (reader.Read())
          {
            int val;
            if (int.TryParse(reader[0].ToString(), out val))
            {
              resourcesList.Add(val);            
            }
          }
        }
        return resourcesList;
      }
    }
    
    /// <summary>
    /// 
    /// </summary>
    private static Structures.Module.IResourcesData HandleUpdateResourcesDataDto(int projectPlanId, int numberVersion)
    {
      var projectPlan = ProjectPlanRXes.GetAll(x => x.Id == projectPlanId).FirstOrDefault();
      if (projectPlan == null)
      {
        throw new Exception(DirRX.ProjectPlanner.Resources.NonExistentPlanIdExeptionFormat(projectPlanId));
      }
      
      var planActivities = ProjectActivities.GetAll(a => ProjectPlanRXes.Equals(a.ProjectPlan, projectPlan) && a.NumberVersion.Value == numberVersion).ToList();
      var planDates = GetProjectPlanDates(projectPlan, planActivities);
      
      var users = new List<Structures.Module.IUser>();
      var projectManager = TryGetProjectManager(projectPlan);
      if (projectManager != null)
      {
        users.Add(projectManager);
      }
      
      var resources = new List<Structures.Module.IResource>();
      var resourceIdList = GetProjectResources(projectPlanId, numberVersion);
      
      FillUsersFromResponsibles(planActivities, users);
      AddEmployeeResources(resourceIdList, users, resources);
      
      return new Structures.Module.ResourcesData
      {
        Users = users,
        Resources = resources,
        Capacity = GetCapacity(resourceIdList, planDates.Start, planDates.End, projectPlanId, numberVersion),
        MaterialResources = GetMaterialResources(),
        ResourceTypes = GetResourcesTypes(),
        WorkingTimeCalendars = GetWorkingTimeCalendars(resourceIdList, planDates.Start, planDates.End)
      };
    }
    
    /// <summary>
    /// Выбирает актуальные даты начала и окончания плана проекта. В карточке плана находятся даты для последней версии плана.
    /// Если надо открыть не последнюю версию плана, то актуальные даты могут отличаться. Поэтому дополнительно ищем даты в списке активити.
    /// </summary>
    /// <param name="projectPlan">План проекта</param>
    /// <param name="activities">Список активити</param>
    /// <returns>Структура с актуальными датами начала и окончания плана</returns>
    private static Structures.Module.ProjectPlanDates GetProjectPlanDates(DirRX.ProjectPlanner.IProjectPlanRX projectPlan, List<DirRX.ProjectPlanner.IProjectActivity> activities)
    {
      var startDateFromCard = Convert.ToDateTime(projectPlan.StartDate);
      //Kiselev HACK в карточке, для удобства отображения, endDate уменьшается на день. Добавляем к endDate день.
      var endDateFromCard = Convert.ToDateTime(projectPlan.EndDate).AddDays(1);
      
      if (startDateFromCard >= endDateFromCard)
      {
        endDateFromCard = startDateFromCard.AddDays(1);
      }
      
      var startDates = new List<DateTime>{startDateFromCard};
      var endDates = new List<DateTime>{endDateFromCard};
      
      startDates.AddRange(activities.Where(a => a.StartDate.HasValue).Select(b => b.StartDate.Value));
      endDates.AddRange(activities.Where(a => a.EndDate.HasValue).Select(b => b.EndDate.Value));
      
      return new Structures.Module.ProjectPlanDates
      {
        Start = startDates.Min(),
        End = endDates.Max()
      };
    }
    
    private static string GetPlanBaselineWorkType(int projectPlanId)
    {
      const string baselineWorkTypeNotSet = "baselineWorkTypeNotSet";
      var projectPlan = ProjectPlanRXes.GetAll(x => x.Id == projectPlanId).FirstOrDefault();
      if (projectPlan == null)
      {
        throw new Exception(DirRX.ProjectPlanner.Resources.NonExistentPlanIdExeptionFormat(projectPlanId));
      }
      return projectPlan.BaselineWorkType.HasValue ? projectPlan.BaselineWorkType.Value.ToString() : baselineWorkTypeNotSet;
    }
    
    /// <summary>
    /// Получаем выходные дни - исключения (выходные в ПН-ПТ).
    /// </summary>
    /// <param name="calendar">Календарь рабочего времени.</param>
    /// <returns>Дополнительные выходные дни.</returns>
    private static List<DateTime> GetExtraFreeDays(Sungero.CoreEntities.IWorkingTimeCalendar calendar)
    {
      //Zheleznov_AV HACK предварительная оптимизация работы с календарями. При получении дат нам надо проверять является ли день рабочим,
      //с учетом частного календаря. Раньше мы использовали метод `d.Day.IsWorkingDay(employee)`. Но это было медленно. Сейчас мы смотрим на d.Duration .
      //
      //Скорее всего есть способ дальнейшей оптимизации. Вроде того, что бы сразу из БД получать нужные данные, a не фильтровать их на уровни прикладной.
      //
      //Данный hack используется еще в методах GetExtraWorkingDays и GetCapacity.
      return calendar.Day.Where(d => 
                                (d.Day.DayOfWeek != DayOfWeek.Saturday && d.Day.DayOfWeek != DayOfWeek.Sunday)
                                && d.Duration == 0)
        .Select(d => d.Day)
        .ToList();
    }
    
    /// <summary>
    /// Получаем рабочие дни - исключения (рабочие в СБ-ВС).
    /// Если суббота или воскресенье становится рабочим днем, то считаем что продолжительность для будет 8 часов.
    /// </summary>
    /// <param name="calendar">Календарь рабочего времени.</param>
    /// <returns>Дополнительные рабочие дни.</returns>
    private static List<Structures.Module.IWorkDay> GetExtraWorkingDays(Sungero.CoreEntities.IWorkingTimeCalendar calendar)
    {
      const int defaultWorkDayDuration = 8;
      return calendar.Day.Where(d => (d.Day.DayOfWeek == DayOfWeek.Saturday || d.Day.DayOfWeek == DayOfWeek.Sunday) && d.Duration > 0)
        .Select(d => new Structures.Module.WorkDay
                {
                  Date = d.Day,
                  Duration = defaultWorkDayDuration
                } as Structures.Module.IWorkDay)
        .ToList();
    }
    
    #region методы, вынесенные из GetModel()
    
    /// <summary>
    /// Создает модель проекта
    /// </summary>
    /// <param name="projectPlan">Проект</param>
    /// <returns>Модель</returns>
    private static DirRX.Planner.Model.Project CreateProjectAppModel(IProjectPlanRX projectPlan)
    {
      var projectApp = new DirRX.Planner.Model.Project();
      projectApp.Id = projectPlan.Id;
      projectApp.Name = projectPlan.Name;
      projectApp.Stage = Sungero.Projects.Projects.Info.Properties.Stage.GetLocalizedValue(projectPlan.Stage);
      projectApp.StartDate = projectPlan.StartDate;
      projectApp.EndDate = projectPlan.EndDate;
      projectApp.BaselineWork = projectPlan.BaselineWork;
      projectApp.ExecutionPercent = projectPlan.ExecutionPercent;
      projectApp.FactualCosts = projectPlan.FactualCosts;
      projectApp.PlannedCosts = projectPlan.PlannedCosts;
      projectApp.Note = projectPlan.Note;
     
      return projectApp;
    }
    
    private static DirRX.ProjectPlanning.IProject GetLinkedProject(DirRX.Planner.Model.Project projectApp, IProjectPlanRX project)
    {
     return DirRX.ProjectPlanning.Projects.GetAll(x => ProjectPlanRXes.Equals(project, x.ProjectPlanDirRX)).FirstOrDefault();
    }
    
    /// <summary>
    /// Добавить менеджера проекта.
    /// </summary>
    /// <param name="projectApp">Проект.</param>
    /// <param name="users">Список пользователей.</param>
    private static void AddManager(DirRX.Planner.Model.Project projectApp, List<DirRX.Planner.Model.Users> users, DirRX.ProjectPlanning.IProject linkedProject)
    {
     if (projectApp.ManagerId == null)
     {
       return;
     }
     
     var managerInfo = new DirRX.Planner.Model.Users();
     managerInfo.Id = linkedProject.Manager.Id;
     managerInfo.Name = linkedProject.Manager.Person.FirstName;
     managerInfo.Surname = linkedProject.Manager.Person.LastName;
     users.Add(managerInfo);
    }
    
    /// <summary>
    /// Создать модель активити.
    /// </summary>
    /// <returns>Модель.</returns>
    private static DirRX.Planner.Model.Activity CreateActivity(IProjectActivity activity, List<DirRX.Planner.Model.Users> users)
    {
      var item = new DirRX.Planner.Model.Activity();
      item.BaselineWork = activity.BaselineWork;
      item.LeadActivityId = activity.LeadingActivity != null ? activity.LeadingActivity.Id : (int?)null;
      item.Id = activity.Id;
      item.StartDate = activity.StartDate;
      item.EndDate = activity.EndDate;
      item.Name = activity.Name;
      item.ExecutionPercent = activity.ExecutionPercent;
      item.Note = activity.Note;
      item.SortIndex = activity.SortIndex.HasValue ? activity.SortIndex.Value : 1;

      item.Predecessors = activity.Predecessors.Where(x => x.Activity != null).Select(x => 
                                                                                      new DirRX.Planner.Model.Predecessor 
                                                                                      {
                                                                                        Id = x.Activity.Id,
                                                                                        LinkType = x.LinkType 
                                                                                      }).ToList();

      item.Priority = activity.Priority;
      item.PlannedCosts = activity.PlannedCosts;
      item.FactualCosts = activity.FactualCosts;
      item.TypeActivity = activity.TypeActivity.ToString();
      item.Status = new DirRX.Planner.Model.ActivityStatus 
      {
        EnumValue = activity.Status.ToString(),
        LocalizeValue = activity.Info.Properties.Status.GetLocalizedValue(activity.Status)
      };
     
      FillResources(activity, item);
      
      AddResponsible(activity, users, item);
      
      return item;
    }
    
    /// <summary>
    /// Заполнить активити ресурсами.
    /// </summary>
    /// <param name="activity">Активити.</param>
    /// <param name="item">Модель активити.</param>
    private static void FillResources(IProjectActivity activity, DirRX.Planner.Model.Activity item)
    {
      foreach(var resource in activity.ResourcesCapacity)
      {
        var resourceData = new DirRX.Planner.Model.ResourcesWorkload();
        resourceData.ResourceId = resource.ResourceId.Value;
        resourceData.Value = resource.Capacity.Value;
        item.Resources.Add(resourceData);
      }
    }
    /// <summary>
    /// Добавить ответственных.
    /// </summary>
    /// <param name="activity">Активити.</param>
    /// <param name="users">Список ответственных.</param>
    private static void AddResponsible(IProjectActivity activity, List<DirRX.Planner.Model.Users> users, DirRX.Planner.Model.Activity item)
    {
      var user = new DirRX.Planner.Model.Users();
      if (activity.Responsible != null)
      {
        item.ResponsibleId = activity.Responsible.Id;
        
        user.Id = activity.Responsible.Id;
        user.Name = activity.Responsible.Person.FirstName;
        user.Surname = activity.Responsible.Person.LastName;
        
        if (!users.Any(x => x.Id == user.Id))
          users.Add(user);
      }
    }
    
    /// <summary>
    /// Заполнить типы ресурсов.
    /// </summary>
    /// <returns>Типы ресурсов.</returns>
    private static List<DirRX.Planner.Model.ResourceTypes> FillResourceTypes()
    {
      var resourceTypes = new List<DirRX.Planner.Model.ResourceTypes>();
      foreach(var type in ProjectResourceTypes.GetAll())
      {
        var resourceType = new DirRX.Planner.Model.ResourceTypes();
        resourceType.Id = type.Id;
        resourceType.Name = type.Name;
        resourceType.SectionName = type.ServiceName;
        resourceTypes.Add(resourceType);
      }
      return resourceTypes;
    }
    
    /// <summary>
    /// Найти человекоресурсы. наверное объединять с следующим методом надо.
    /// </summary>
    /// <param name="projActs">Активити.</param>
    /// <param name="users">Пользователи проекта.</param>
    /// <param name="resources">Ресурсы.</param>
    private static void FindEmployeeResources(List<IProjectActivity> projActs, List<DirRX.Planner.Model.Users> users, List<DirRX.Planner.Model.Resources> resources)
    {
      Logger.Debug("Find employee Resource.");
      
      var firstAct = projActs.FirstOrDefault();
      if (firstAct == null)
      {
        return;
      }
      
      var resourcesList = GetProjectResources(firstAct.ProjectPlan.Id, firstAct.NumberVersion.Value);
      //TODO URMANOV: тут такой же запрос как в UpdateActivityResources на получение ид ресурсов в проекте
      foreach (var employeeResource in ProjectsResources.GetAll(x => x.Type.ServiceName == Constants.Module.ResourceTypes.Users
                                                                && resourcesList.Contains(x.Id)))
      {
        Logger.Debug("Finded employee Resource.");
        var user = new DirRX.Planner.Model.Users();
        var resourceLink = new DirRX.Planner.Model.Resources();
        
        user.Id = employeeResource.Employee.Id;
        //user.Position = employeeResource.Employee.JobTitle != null ? employeeResource.Employee.JobTitle.Name : string.Empty;
        user.Name = employeeResource.Employee.Person != null ? employeeResource.Employee.Person.FirstName : employeeResource.Employee.Name;
        user.Surname = employeeResource.Employee.Person != null ? employeeResource.Employee.Person.LastName : string.Empty;
        
        resourceLink.Id = employeeResource.Id;
        resourceLink.EntityTypeId = employeeResource.Type.Id;
        resourceLink.EntityId = employeeResource.Employee.Id;
        resourceLink.UnitLabel = employeeResource.Type.MeasureUnit;
        
        if(!users.Any(x => x.Id == user.Id))
          users.Add(user);
        
        resources.Add(resourceLink);
        Logger.DebugFormat("employee {0} Resource {1} added.", user.Id, resourceLink.Id);
      }
    }
    
    /// <summary>
    /// Найти материальные ресурсы.
    /// </summary>
    /// <param name="projActs">Активити.</param>
    /// <param name="resources">Ресурсы.</param>
    /// <returns></returns>
    private static List<DirRX.Planner.Model.MaterialResources> FindMaterialResources(List<IProjectActivity> projActs, List<DirRX.Planner.Model.Resources> resources)
    {
      var firstAct = projActs.FirstOrDefault();
      if (firstAct == null)
      {
        return null;
      }
      
      var resourcesList = GetProjectResources(firstAct.ProjectPlan.Id, firstAct.NumberVersion.Value);
      
      var materialResources = new List<DirRX.Planner.Model.MaterialResources>();
      Logger.Debug("Find material Resource.");
      foreach (var resource in ProjectsResources.GetAll(x => x.Type.ServiceName == Constants.Module.ResourceTypes.MaterialResources
                                                        && resourcesList.Contains(x.Id)))
      {
        Logger.Debug("Finded material Resource.");
        var materialResource = new DirRX.Planner.Model.MaterialResources();
        var resourceLink = new DirRX.Planner.Model.Resources();
        
        materialResource.Id = resource.Id;
        materialResource.Name = resource.Name;
        
        resourceLink.Id = resource.Id;
        resourceLink.EntityTypeId = resource.Type.Id;
        resourceLink.EntityId = resource.Id;
        resourceLink.UnitLabel = resource.Type.MeasureUnit;
        
        materialResources.Add(materialResource);
        resources.Add(resourceLink);
        Logger.DebugFormat("resource {0} Resource {1} added.", materialResource.Id, resourceLink.Id);
      }
      return materialResources;
    }
    
    /// <summary>
    /// Заполнить длительность проекта.
    /// </summary>
    /// <param name="project">Проект (только для получения дат).</param>
    /// <param name="resources">Ресурсы.</param>
    /// <returns></returns>
    private static List<DirRX.Planner.Model.Capacity> FillCapacities(IProjectPlanRX projectPlan, List<DirRX.Planner.Model.Resources> resources, int numberVersion, IProjectActivity activity)
    {
      var startDate = Convert.ToDateTime(projectPlan.StartDate);
      var endDate = Convert.ToDateTime(projectPlan.EndDate);
      var capacities = new List<DirRX.Planner.Model.Capacity>();
      using (var connection = CreateDBConnection())
      {
      foreach(var resource in resources)
      {
        Logger.Debug("Filling capacity.");
        var capacity = new DirRX.Planner.Model.Capacity();
        capacity.ResourceId = resource.Id;
        var values = new List<DirRX.Planner.Model.Value>();
        var calendars = GetPrivateOrPublicCalendars(startDate, endDate, resource.Id);
        var dates = new List<Sungero.CoreEntities.IWorkingTimeCalendarDay>();
        foreach (var calendar in calendars)
        {
          dates.AddRange(calendar.Day.Where(x => startDate <= x.Day && x.Day <= endDate).ToList());
        }
        var busyes = GetResourceAverageBusy(projectPlan.Id, resource.Id, numberVersion, startDate, endDate, connection);
        
        foreach(var date in dates)
        {
          var val = new DirRX.Planner.Model.Value();
          val.Date = date.Day;
          val.Busy = GetDailyAverageBusy(busyes, date.Day);
          values.Add(val);
        }
        capacity.Values = values;
        capacities.Add(capacity);
      }
      return capacities;
      }
    }
    
    

    
    /// <summary>
    /// Заполнить календари рабочего времени.
    /// </summary>
    /// <param name="project">Проект (только для получения периода дат).</param>
    /// <param name="resources">Ресурсы.</param>
    /// <returns></returns>
    private static List<DirRX.Planner.Model.WorkingTimeCalendar> FillWorkingCalendars(IProjectPlanRX project, List<DirRX.Planner.Model.Resources> resources)
    {
      var startDate = Convert.ToDateTime(project.StartDate);
      var endDate = Convert.ToDateTime(project.EndDate);
      
      var workingTimeCalendars = new List<DirRX.Planner.Model.WorkingTimeCalendar>();
      foreach(var resource in resources)
      {
        Logger.Debug("Filling workingTimeCalendars.");
        var workingTimeCalendar = new DirRX.Planner.Model.WorkingTimeCalendar();
        
        var calendars = GetPrivateOrPublicCalendars(startDate, endDate, resource.Id);
        
        foreach(var calendar in calendars)
        {
          var freeDays = calendar.Day.Where(d => d.Kind.HasValue).Select(d => d.Day);
          var workDays = calendar.Day.Where(d => !d.Kind.HasValue).Select(d => new DirRX.Planner.Model.WorkDays()
                                                                          {
                                                                            Date = d.Day,
                                                                            Duration = d.Duration.Value
                                                                          });
          workingTimeCalendar.WorkDays.AddRange(workDays);
          workingTimeCalendar.FreeDays.AddRange(freeDays);
        }
        
        workingTimeCalendar.ResourcesIds.Add(resource.Id);
        workingTimeCalendars.Add(workingTimeCalendar);
     }
      return workingTimeCalendars;
    }
    #endregion
    
    private static int GetLastActivityId(List<IProjectActivity> projActs)
    {
      //Zheleznov_AV HACK изначально тут был метод, получающий lastActivtyId через анализ истории сущностей.
      //Он работал медленно. В рамках оптимизации изменил метод на "однострочник". Не делаю inline-ing данного метода,
      //что бы не менять текущий API.
      return projActs.Any() ? projActs.Max(x => x.Id) : 0;
    }
    
    public static string UpdateModel(IProjectPlanRX project, int numberVersion, DirRX.Planner.Model.Model model)
    {
      var prActs = ProjectActivities.Create();
      var listStatuses = new List<DirRX.Planner.Model.ActivityStatus>();
      foreach (Enumeration i in prActs.StatusAllowedItems)
      {
        listStatuses.Add(new DirRX.Planner.Model.ActivityStatus {EnumValue = i.ToString(), LocalizeValue = prActs.Info.Properties.Status.GetLocalizedValue(i)});
      }
     
      model.ActivityStatuses = listStatuses;
      
      var projActs = ProjectActivities.GetAll(a => ProjectPlanRXes.Equals(a.ProjectPlan, project) && a.NumberVersion.Value == numberVersion).ToList();
      model.LastActivityId = GetLastActivityId(projActs);

      var modelJson = DirRX.Planner.Model.Serialization.Serializable.PackToJson(model);
      
      return modelJson;
    }
    
    [Remote, Public(WebApiRequestType = RequestType.Get)]
    public static string GetModel(int projectPlanId, int numberVersion)
    {
      var projectPlan = ProjectPlanRXes.Get(projectPlanId);
      return GetModel(projectPlan, numberVersion);
    }
    
    /// <summary>
    /// Передает данные по проекту в модель Json.
    /// </summary>
    /// <param name="projectId">ИД проекта.</param>
    /// <returns>Сериализованная строка с данными по проекту.</returns>
    [Remote, Public]
    public static string GetModel(IProjectPlanRX project, int numberVersion)
    {
      var model = new DirRX.Planner.Model.Model();
      var projActs = ProjectActivities.GetAll(a => ProjectPlanRXes.Equals(a.ProjectPlan, project) && a.NumberVersion.Value == numberVersion).ToList();
      
      var activitiesApp = new List<DirRX.Planner.Model.Activity>();
      var resources = new List<DirRX.Planner.Model.Resources>();
      var users = new List<DirRX.Planner.Model.Users>();
      
      AccessRights.AllowRead(() =>
                             {
                               var projectApp = CreateProjectAppModel(project);
                               projectApp.Note = project.Note;
                               
                               foreach (var activity in projActs)
                               {
                                 var item = CreateActivity(activity, users);
                                 
                                 activitiesApp.Add(item);
                               }
                               var resourceTypes = FillResourceTypes();
                              
                               var linkedProject = GetLinkedProject(projectApp, project);
                               
                               if (linkedProject != null)
                               {
                                 projectApp.ManagerId = linkedProject.Manager.Id;
                                 projectApp.UseBaseLineWorkInHours = linkedProject.BaselineWorkType != DirRX.ProjectPlanning.Project.BaselineWorkType.Money;
                               }
                               
                               AddManager(projectApp, users, linkedProject);
                               
                               FindEmployeeResources(projActs, users, resources);
                               
                               var materialResources = FindMaterialResources(projActs, resources);
                               
                               List<DirRX.Planner.Model.Capacity> capacities = new List<DirRX.Planner.Model.Capacity>();
                               
                               foreach (var act in projActs)
                               {
                                 capacities.AddRange(FillCapacities(project, resources, numberVersion, act));
                               }
                               
                               var workingTimeCalendars = FillWorkingCalendars(project, resources);
                               
                               model.LastActivityId = GetLastActivityId(projActs);
                               
                               var prAct = ProjectActivities.Create();
                               var listStatuses = new List<DirRX.Planner.Model.ActivityStatus>();
                               foreach (Enumeration i in prAct.StatusAllowedItems)
                               {
                                 listStatuses.Add(new DirRX.Planner.Model.ActivityStatus {EnumValue = i.ToString(), LocalizeValue = prAct.Info.Properties.Status.GetLocalizedValue(i)});
                               }
                               
                               model.ActivityStatuses = listStatuses;
                               model.Project = projectApp;
                               model.Activities = activitiesApp.ToList();
                               Logger.Debug("Filling resourcesData.");
                               var resourcesData = new DirRX.Planner.Model.ResourcesData();
                               resourcesData.Capacity = capacities;
                               resourcesData.MaterialResources = materialResources;
                               resourcesData.Resources = resources;
                               resourcesData.ResourceTypes = resourceTypes;
                               resourcesData.Users = users;
                               resourcesData.WorkingTimeCalendars = workingTimeCalendars;
                               model.ResourcesData = resourcesData;
                             });
      
      var modelJson = DirRX.Planner.Model.Serialization.Serializable.PackToJson(model);
      
      return modelJson;
    }
    
    /// <summary>
    /// Отправка задачи по этапу проекта.
    /// </summary>
    /// <param name="activityId">Id этапа</param>
    /// <returns>Задача</returns>
    [Remote, Public]
    public static IProjectActivityTask CreateTask(int activityId)
    {
      var activity = ProjectPlanner.ProjectActivities.Get(activityId);
      var subject = string.Format(Resources.SEND_TASK_SUBJECT, activity.Name);
      var newTask = ProjectActivityTasks.Create();
      if(activity.Performers != null && activity.Performers.Count > 0)
      {
        foreach (var performer in activity.Performers)
        {
          var newResponsible = newTask.Responsibles.AddNew();
          newResponsible.Responsible = performer.Performer;
        }
      }
      
      if (activity.Responsible != null)
      {
          var newResponsible = newTask.Responsibles.AddNew();
          newResponsible.Responsible = activity.Responsible;
      }
      
      newTask.Subject = subject;
      
      //Отнимаем один календарный день, потому что дата окончания активити в коде равна 00:00 следующего дня, 
      //что не соответствует визуальному отображению в клиенте.
      var activityEndDate = (activity.EndDate ?? activity.StartDate ?? Calendar.Today).AddDays(-1);
      //Для того чтобы точно быть уверенными, что мы берем дату без времени - берем свойство Date у активити,
      //не смотря на то, что в нормальных ситуациях там будет DateTime с временем 00:00.
      newTask.MaxDeadline = activityEndDate.Date;
      newTask.ActiveText = Resources.SEND_TASK_ACTIVE_TEXT;
      newTask.ProjectActivity = activity;
      newTask.ProjectPlan = activity.ProjectPlan;
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
      var result = string.Empty;
      AccessRights.AllowRead(() =>
                             {
                               try
                               {
                                 var webAdressGetter = new WebClientAddressGetter.WebClientAddressGetter();
                                 result = webAdressGetter.GetContentFullAddress(new Uri(Constants.Module.DefaultClientAddress, UriKind.Relative)).ToString();
                               }
                               catch (WebClientAddressGetter.Exceptions.SungeroConfigSettingsException ex)
                               {
                                 throw new Exception(DirRX.ProjectPlanner.Resources.ErrorWhileTryOpenWebClient, ex);
                               }
                               
                             });
      return result;
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
    
    /// <summary>
    /// 
    /// </summary>
    public static List<Structures.Module.ICapacity> GetCapacity(List<int> resourceIds, DateTime startDate, DateTime endDate, int planId, int planVersion)
    {
      using (var connection = CreateDBConnection())
      {
        var capacities = new List<Structures.Module.ICapacity>();
        foreach(var resourceId in resourceIds)
        {
          //получаем сущность ресурса, передаем в календари
          var capacity =  new Structures.Module.Capacity()
          {
            Values = new List<DirRX.ProjectPlanner.Structures.Module.ICapacityValue>(),
            ResourceId = resourceId
          };
          var calendars = GetPrivateOrPublicCalendars(startDate, endDate, resourceId);
          var dates = new List<Sungero.CoreEntities.IWorkingTimeCalendarDay>();
          foreach(var calendar in calendars)
          {
            dates.AddRange(calendar.Day.Where(x => startDate <= x.Day && x.Day < endDate && x.Duration > 0));
          }
          var busyes = GetResourceAverageBusy(planId, resourceId, planVersion, startDate, endDate, connection);
          foreach(var date in dates)
          {
            capacity.Values.Add(new Structures.Module.CapacityValue()
                                       {
                                         Date = date.Day,
                                         Busy = GetDailyAverageBusy(busyes, date.Day)
                                       });
          }
          capacities.Add(capacity);
        }
        return capacities;
      }
    }
    
    private static double GetDailyAverageBusy(List<Structures.Module.AverageBusy> busyes, DateTime date)
    {
      return busyes.Where(b => b.EndDate > date && b.StartDate <= date).Sum(b => b.AvgBusy);
    }
    
    private static List<Structures.Module.AverageBusy> GetResourceAverageBusy(int planId, int resourceId, int numberVersion, DateTime startDate, DateTime endDate, System.Data.IDbConnection connection)
    {
      List<Structures.Module.AverageBusy> result = new List<DirRX.ProjectPlanner.Structures.Module.AverageBusy>();
      
      //получить список активити (дата начала, конца, загрузка) в выбранный период для ресурса
      using (var command = connection.CreateCommand())
      {
        command.CommandText = "select rl.average_busy, pa.StartDate, pa.EndDate " +
          "from ResourceLinks rl " +
          "join DirRX_Projec1_PrjctActivity pa on pa.id = rl.project_activity_id " +
          "where NOT(pa.ProjectPlan = @projectPlanId AND pa.NumberVersion = @VersionNum) AND rl.resource_id = @ResourceId AND (pa.EndDate >= @StartDate AND @EndDate >= pa.StartDate)";
        SQL.AddParameter(command, "@projectPlanId", planId, System.Data.DbType.Int32);
        SQL.AddParameter(command, "@VersionNum", numberVersion, System.Data.DbType.Int32);
        SQL.AddParameter(command, "@ResourceId", resourceId, System.Data.DbType.Int32);
        SQL.AddParameter(command, "@StartDate", startDate, System.Data.DbType.DateTime);
        SQL.AddParameter(command, "@EndDate", (endDate == default) ? startDate : endDate , System.Data.DbType.DateTime);
        
        using (var reader = command.ExecuteReader())
        {
          while (reader.Read())
          {
            result.Add(new Structures.Module.AverageBusy() { AvgBusy = (float)reader[0], StartDate = (DateTime)reader[1], EndDate = (DateTime)reader[2] });
          }
        }
      }
      return result;
    }
    
    /// <summary>
    /// 
    /// </summary>
    public static List<Structures.Module.IWorkingTimeCalendar> GetWorkingTimeCalendars(List<int> resourceIds, DateTime startDate, DateTime endDate)
    {
      var workingTimeCalendar = new List<Structures.Module.IWorkingTimeCalendar>();
      foreach(var resourceId in resourceIds)
      {
        var workingTime = new Structures.Module.WorkingTimeCalendar(){ResourcesIds = new List<int>(){resourceId}};
        var calendars = GetPrivateOrPublicCalendars(startDate, endDate, resourceId);
        
        foreach (var calendar in calendars)
        {
          workingTime.FreeDays = GetExtraFreeDays(calendar);
          workingTime.WorkDays = GetExtraWorkingDays(calendar);
        }
        
        workingTimeCalendar.Add(workingTime);
      }
      return workingTimeCalendar;
    }
    
    /// <summary>
    /// Получить календарь
    /// </summary>
    /// <param name="startDate">Дата начала.</param>
    /// <param name="endDate">Дата окончания.</param>
    /// <param name="resourceId">id ресурса</param>
    /// <returns>Календарь рабочего времени.</returns>
    public static IWorkingTimeCalendar[] GetPrivateOrPublicCalendars(DateTime startDate, DateTime endDate, int resourceId)
    {
      var resource = ProjectsResources.Get(resourceId);
      List<IWorkingTimeCalendar> calendars = new List<IWorkingTimeCalendar>();
      
      for (int year = startDate.Year; year <= endDate.Year; year++)
      {
        IWorkingTimeCalendar calendar;
        //Если ресурс - сотрудник, поиск календаря для сотрудника. Сначала ищем персональный, в случае его отсутствия - возвращается общий.
        if (resource.Employee != null)
          calendar = PrivateWorkingTimeCalendars.GetAll(x => x.Year.Value == year && x.Recipients.Any(y => y.Recipient.Id == resource.Employee.Id)).FirstOrDefault();
        else
          calendar = PrivateWorkingTimeCalendars.GetAll(x => x.Year.Value == year && x.Recipients.Any(y => y.Recipient.Id == resource.Id)).FirstOrDefault();
        
        if (calendar == null)
          calendar = Sungero.CoreEntities.WorkingTime.GetAll(x => x.Year.Value == year && !PrivateWorkingTimeCalendars.Is(x)).FirstOrDefault();
        
        if (calendar == null)
        {
          continue;
        }
        
        calendars.Add(calendar);
      }
      
      return calendars.ToArray();
    }
    
    /// <summary>
    /// [Для демо - генератора] Добавление ресурса. 
    /// </summary>
    /// <param name="activityId">Идентификатор этапа. </param>
    /// <param name="resourceId">Идентификатор ресурса. </param>
    /// <param name="workload">Трудоемкость. </param>
    /// <param name="projectPlanId">Идентификатор плана. </param>
    [Public(WebApiRequestType = RequestType.Post), Remote]
    public void AddResourcesDemo(int activityId, int resourceId, int workload, int projectPlanId)
    {
      var activity = ProjectActivities.GetAll(a => a.Id == activityId).FirstOrDefault();
      var resource = ProjectsResources.GetAll(r => r.Id == resourceId).FirstOrDefault();
      var projectPlan = ProjectPlanRXes.GetAll(p => p.Id == projectPlanId).FirstOrDefault();
      
      if (resource == null || activity == null)
      {
        return;
      }
      
      var res1 = activity.ResourcesCapacity.AddNew();
      res1.Capacity = workload;
      res1.ResourceId = resource.Id;
      
      using (var connection = CreateDBConnection())
      {
        var act = new DirRX.Planner.Model.Activity()
        {
          Resources = new List<DirRX.Planner.Model.ResourcesWorkload>(),
          StartDate = activity.StartDate,
          EndDate = activity.EndDate,
          Id = activity.Id
        };
        act.Resources.Add(new DirRX.Planner.Model.ResourcesWorkload()
                          {
                            ResourceId = resource.Id,
                            Value = workload
                          });
        SaveResources(act.Resources, act.Id.Value, act.StartDate.Value, act.EndDate.Value, connection);
        activity.Save();
        connection.Close();
      }
      DirRX.ProjectPlanner.PublicFunctions.Module.Remote.WriteJsonBodyToProjectVersion(projectPlan, projectPlan.LastVersion.Number.Value, false);
    }
    
  }
}