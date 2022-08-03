using System;
using System.Collections.Generic;
using System.Linq;
using Sungero.Core;
using Sungero.CoreEntities;

namespace DirRX.ProjectPlanner.Client
{
  public class ModuleFunctions
  { 
    /// <summary>
    /// Снятие блокировки с карточки проекта.
    /// </summary>
    /// <param name="projectId">Id проекта</param>
    [Hyperlink]
    public void UnlockProject(string projectId)
    {
      var pp = Functions.ProjectPlanRX.Remote.GetProjectPlan(int.Parse(projectId));
      if (pp == null)
        return;
      
      var linkedProject = Functions.ProjectPlanRX.Remote.GetLinkedProject(pp);
      
      if (!Functions.Module.Remote.GetOpensProjectPlansFromCard(pp, linkedProject).Any())
      {
        var ppLockInfo = Locks.GetLockInfo(pp);
        if(ppLockInfo != null && ppLockInfo.IsLockedByMe)
          Locks.Unlock(pp);
        
        if (linkedProject != null)
        {
          var projectLockInfo = Locks.GetLockInfo(linkedProject);
          if (projectLockInfo != null && projectLockInfo.IsLockedByMe)
            Locks.Unlock(linkedProject);
        }
      }
    }
    
    [Hyperlink]
    public void UpdateActivity(string projectId)
    {
      var pp = Functions.ProjectPlanRX.Remote.GetProjectPlan(int.Parse(projectId));
      if (pp == null)
        return;
      
      var linkedProject = Functions.ProjectPlanRX.Remote.GetLinkedProject(pp);
      
      try
      {
        var successfullyLocked = Locks.TryLock(pp);
        // Zheleznov_AV HACK если удалось заблокировать карточку плана проекта, значит либо план проекта был открыт из списка,
        // либо он был открыт из карточки, но карточка уже закрыта. В обоих случаях справочник OpensProjectPlansFromCard не должен
        // содержать записей о данном плане.
        if (successfullyLocked)
          Functions.OpensProjectPlansFromCard.Remote.DeleteEntry(pp);
        
        if (linkedProject != null)
          Locks.TryLock(linkedProject);
      }
      catch (Exception ex)
      {
        Logger.DebugFormat("Ошибка в UpdateActivity: {0}", ex.Message);
      }
    }
    
    /// <summary>
    /// Запускает приложение планирования проекта.
    /// </summary>
    /// <param name="webSite">Сайт сервиса.</param>
    /// <param name="projectId">ИД проекта.</param>
    /// <param name="numVersion">Номер версии документа.</param>
    /// <param name="employeeId">Ид сотрудника.</param>
    /// <param name="isReadOnly">Открыть проект в режиме чтения.</param>
    [Public]
    public virtual void RunPlannerApp(string webSite, int projectId, int numVersion, int employeeId, bool isReadOnly)
    {
      if (webSite != null)
        GoToWebsite(webSite, projectId, 0, numVersion, employeeId, isReadOnly);
      else
        Dialogs.ShowMessage(DirRX.ProjectPlanner.Resources.WebClientURIEmptyError, MessageType.Warning);
    }
    
    /// <summary>
    /// Запускает приложение планирования проекта.
    /// </summary>
    /// <param name="webSite">Сайт сервиса.</param>
    /// <param name="projectId">ИД проекта.</param>
    /// <param name="activityId">ИД активити.</param>
    /// <param name="numVersion">Номер версии документа.</param>
    /// <param name="employeeId">Ид сотрудника.</param>
    /// <param name="isReadOnly">Открыть проект в режиме чтения.</param>
    [Public]
    public virtual void RunPlannerApp(string webSite, int projectId,int activityId, int numVersion, int employeeId, bool isReadOnly)
    {
      if (webSite != null)
        GoToWebsite(webSite, projectId, activityId, numVersion, employeeId, isReadOnly);
      else
        Dialogs.ShowMessage(DirRX.ProjectPlanner.Resources.WebClientURIEmptyError, MessageType.Warning);
    }
    
    /// <summary>
    /// Создание задачи по этапу проекта.
    /// </summary>
    /// <param name="activityId">Id этапа</param>
    [Hyperlink]
    public static void CreateTask(string activityId)
    {
      var task = Functions.Module.Remote.CreateTask(int.Parse(activityId));
      task.Show();
    }
    
    /// <summary>
    /// Открытие клиента по планированию проектов.
    /// </summary>
    /// <param name="website">Адрес клиента</param>
    /// <param name="projectId">Id проекта</param>
    /// <param name="activityId">Id этапа</param>
    /// <param name="numVersion">Номер версии</param>
    /// <param name="userId">Id пользователя</param>
    /// <param name="isReadOnly">Только чтение</param>
    public static void GoToWebsite(string website, int projectId, int activityId,int numVersion, int userId, bool isReadOnly)
    {
      // TODO: Отрефакторить с нормальным строителем запросов.
      var projectParam = System.Net.WebUtility.UrlEncode(string.Format("{0}", projectId ));
      var readonlyParam = System.Net.WebUtility.UrlEncode(string.Format("{0}",  isReadOnly ? isReadOnly : IsReadOnlyProject(projectId, userId)));
      var numVersionParam = System.Net.WebUtility.UrlEncode(string.Format("{0}",  numVersion == 0 ? Functions.ProjectPlanRX.Remote.GetProjectPlan(projectId).LastVersion.Number : numVersion));
      website = string.Format("{0}?projectId={1}&readonly={2}&numberVersion={3}", website.ToLower(), projectParam, readonlyParam, numVersionParam);
      
      if (activityId != 0)
      {
        var activityParam = System.Net.WebUtility.UrlEncode(string.Format("{0}", activityId ));
        website = string.Format("{0}&activityId={1}", website, activityParam);
      }
      
      Hyperlinks.Open(website);
    }
    
    /// <summary>
    /// Показывает список задач по проекту.
    /// </summary>
    /// <param name="tasksIds">Список ИД задач по проекту.</param>
    [Public]
    public static void ShowProjectTasks(List<int> tasksIds)
    {
      var tasks = Functions.Module.Remote.GetTasksByIds(tasksIds);
      tasks.Show();
    }
    
    /// <summary>
    /// Проверка наличия лицензии на планирование проектов.
    /// </summary>
    /// <returns>True - наличие лицензии, False - отсутствие лицензии.</returns>
    [Public]
    public bool CheckProjectPlannerLicence()
    {
      return Sungero.Docflow.PublicFunctions.Module.Remote.IsModuleAvailableByLicense(Constants.Module.ProjectPlannerModuleGuid);
    }
    
    /// <summary>
    /// Определение режима доступа к проекту.
    /// </summary>
    /// <param name="projectId">Id проекта</param>
    /// <param name="userId">Id пользователя</param>
    private static bool IsReadOnlyProject(int projectId, int userId)
    {
      var project = Functions.ProjectPlanRX.Remote.GetProjectPlan(projectId);
      return !project.AccessRights.CanUpdate();
    }
    
    /// <summary>
    /// Снятие блокировки с карточки проекта.
    /// </summary>
    private static void Unlock(int projectId, int loginId)
    {
      var project = Functions.ProjectPlanRX.Remote.GetProjectPlan(projectId);
      var lockInfo = Locks.GetLockInfo(project);
      if (lockInfo.LoginId == loginId && lockInfo.IsLocked)
        Locks.Unlock(project);
    }
    
  }
}