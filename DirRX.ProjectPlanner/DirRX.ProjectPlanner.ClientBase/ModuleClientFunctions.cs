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
		/// Сохранить модель по ссылке.
		/// </summary>
		/// <param name="binaryBodyId">Идентификатор бинарного тела в сервисе хранилищ.</param>
		[Hyperlink]
		public void SaveModel(string binaryBodyId, string projectId, string version)
		{
			Functions.Module.Remote.SaveModelFromStorageByBinaryBodyId(binaryBodyId, int.Parse(projectId), int.Parse(version));
		}
		
		[Hyperlink]
		public void UnlockProject(string projectId)
		{
			var pp = ProjectPlans.Get(int.Parse(projectId));
			var linkedProject = DirRX.ProjectPlanning.Projects.GetAll(x => ProjectPlans.Equals(pp, x.ProjectPlanDirRX)).FirstOrDefault();
			
			if (!Functions.Module.Remote.GetOpensProjectPlansFromCard(pp, linkedProject).Any())
			{
				Locks.Unlock(pp);
				if (linkedProject != null)
					Locks.Unlock(linkedProject);
			}
		}
		
		[Hyperlink]
		public void UpdateActivity(string projectId)
		{
			var pp = ProjectPlans.Get(int.Parse(projectId));
			var linkedProject = Functions.ProjectPlan.Remote.GetLinkedProejct(pp);
			
			try
			{
				Locks.TryLock(pp);
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
			GoToWebsite(webSite, projectId, 0, numVersion, employeeId, isReadOnly);
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
			GoToWebsite(webSite, projectId, activityId, numVersion, employeeId, isReadOnly);
		}
		
		[Hyperlink]
		public static void CreateTask(string activityId)
		{
			var task = Functions.Module.Remote.CreateTask(int.Parse(activityId));
			task.Show();
		}
		
		public static void GoToWebsite(string website, int projectId, int activityId,int numVersion, int userId, bool isReadOnly)
		{
			if(!(website.StartsWith("http://") || website.StartsWith("https://")))
			{
				website = "http:// " + website;
			}
			
			// TODO: Отрефакторить с нормальным строителем запросов.
			var projectParam = System.Net.WebUtility.UrlEncode(string.Format("{0}", projectId ));
			var readonlyParam = System.Net.WebUtility.UrlEncode(string.Format("{0}",  isReadOnly ? isReadOnly : IsReadOnlyProject(projectId, userId)));
			var numVersionParam = System.Net.WebUtility.UrlEncode(string.Format("{0}",  numVersion == 0 ? Functions.ProjectPlan.Remote.GetProjectPlan(projectId).LastVersion.Number : numVersion));
			website = string.Format("{0}?projectId={1}&readonly={2}&numberVersion={3}", website.ToLower(), projectParam, readonlyParam, numVersionParam);
			
			if (activityId != 0)
			{
				var activityParam = System.Net.WebUtility.UrlEncode(string.Format("{0}", activityId ));
				website = string.Format("{0}&activityId={1}", website, activityParam);
			}
			
			Hyperlinks.Open(website);
		}
		
		
		
		/// <summary>
		/// Отправляет задачи по выбранным этапам.
		/// </summary>
		/// <param name="activityIds">Список ид этапов.</param>
		/// <param name="isShowCards">Отображать ли карточки задач.</param>
		[Public]
		public void SendTasks(List<int> activityIds, bool isShowCards)
		{
			var tasks = Functions.Module.Remote.CreateTasks(activityIds);
			foreach (var task in tasks)
			{
				if (isShowCards)
					task.Show();
				else
					task.Start();
			}
			
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
		
		private static bool IsReadOnlyProject(int projectId, int userId)
		{
			var project = Functions.ProjectPlan.Remote.GetProjectPlan(projectId);
			return !project.AccessRights.CanUpdate();
		}
		
		private static void Unlock(int projectId, int loginId)
		{
			var project = Functions.ProjectPlan.Remote.GetProjectPlan(projectId);
			var lockInfo = Locks.GetLockInfo(project);
			if (lockInfo.LoginId == loginId && lockInfo.IsLocked)
				Locks.Unlock(project);
		}
		
	}
}