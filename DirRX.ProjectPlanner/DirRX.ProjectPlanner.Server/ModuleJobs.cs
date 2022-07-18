using System;
using System.Collections.Generic;
using System.Linq;
using Sungero.Core;
using Sungero.CoreEntities;

namespace DirRX.ProjectPlanner.Server
{
  public class ModuleJobs
  {

    public virtual void SyncAccessRightsProjectPlanAndProject()
    {
      var needUpdateLastRunDate = true;
      var previousRun = GetLastSyncAccessRightsDate();
      var startDate = Calendar.Now;
      
      var projects = DirRX.ProjectPlanning.Projects.GetAll(x => x.ProjectPlanDirRX != null &&
                                                           x.Modified >= previousRun &&
                                                           x.Modified < startDate);
      
      
      foreach (var project in projects)
      {
        try
        {
          var projectPlan = project.ProjectPlanDirRX;
          if (projectPlan != null)
          {
            var oldRights = projectPlan.AccessRights.Current;
            var newRights = project.AccessRights.Current;

            
            foreach (var oldRight in oldRights)
            {
              if(!newRights.Contains(oldRight))
              {
                if (!Locks.GetLockInfo(projectPlan).IsLockedByOther)
                {
                  projectPlan.AccessRights.Revoke(oldRight.Recipient, oldRight.AccessRightsType);
                  projectPlan.AccessRights.Save();
                }
              }
            }
            
            foreach (var newRight in newRights)
            {
              if(!oldRights.Contains(newRight))
              {
                if (!Locks.GetLockInfo(projectPlan).IsLockedByOther)
                {
                  projectPlan.AccessRights.Grant(newRight.Recipient, newRight.AccessRightsType);
                  projectPlan.AccessRights.Save();
                }
              }
            }
          }
        }

        catch (Exception ex)
        {
          Logger.DebugFormat("Произошли ошибка в ФП SyncAccessRightsProjectPlanAndProject при обработке проекта {0} - {1}: {2}", project.Id, project.DisplayValue, ex.Message);
          needUpdateLastRunDate = false;
        }
        
      }
      
      if (needUpdateLastRunDate == true)
        UpdateLastSyncAccessRightsDate(startDate);
    }
    

    
    /// <summary>
    /// Получить дату последней рассылки уведомлений.
    /// </summary>
    /// <returns>Дата последней рассылки.</returns>
    public static DateTime GetLastSyncAccessRightsDate()
    {
      var key = "LastSyncAccessRightsOfProject";
      var command = string.Format(Queries.Module.SelectDocflowParamsValue, key);
      try
      {
        var executionResult = Sungero.Docflow.PublicFunctions.Module.ExecuteScalarSQLCommand(command);
        var date = string.Empty;
        if (!(executionResult is DBNull) && executionResult != null)
          date = executionResult.ToString();
        else
          return Calendar.Today;
        
        Logger.DebugFormat("Last sync date in DB is {0} (UTC)", date);
        
        DateTime result = Calendar.FromUtcTime(DateTime.Parse(date, null, System.Globalization.DateTimeStyles.AdjustToUniversal));
        return result;
      }
      catch (Exception ex)
      {
        Logger.Error("Error while getting last sync date", ex);
        return Calendar.Today;
      }
    }
    
    /// <summary>
    /// Обновить дату последней рассылки уведомлений.
    /// </summary>
    /// <param name="notificationDate">Дата рассылки уведомлений.</param>
    public static void UpdateLastSyncAccessRightsDate(DateTime notificationDate)
    {
      var key = "LastSyncAccessRightsOfProject";
      
      var newDate = notificationDate.Add(-Calendar.UtcOffset).ToString("yyyy-MM-ddTHH:mm:ss.ffff+0");
      Sungero.Docflow.PublicFunctions.Module.ExecuteSQLCommandFormat(Queries.Module.InsertOrUpdateDocflowParamsValue, new[] { key, newDate });
      Logger.DebugFormat("Last sync date is set to {0} (UTC)", newDate);
    }

  }
}