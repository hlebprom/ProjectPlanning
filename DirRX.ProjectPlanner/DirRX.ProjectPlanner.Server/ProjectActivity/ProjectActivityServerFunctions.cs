using System;
using System.Collections.Generic;
using System.Linq;
using Sungero.Core;
using Sungero.CoreEntities;
using DirRX.ProjectPlanner.ProjectActivity;

namespace DirRX.ProjectPlanner.Server
{
  partial class ProjectActivityFunctions
  {
    
    /// <summary>
    /// Возвращает этапы по проекту.
    /// </summary>
    /// <param name="project">Проект.</param>
    /// <returns>Этапы по проекту.</returns>
    [Remote, Public]
    public static IQueryable<IProjectActivity> GetActivities(IProjectPlan projectPlan)
    {
      return ProjectActivities.GetAll().Where(c => ProjectPlans.Equals(c.ProjectPlan, projectPlan));
    }
    
    /// <summary>
    /// Возвращает подчиненные этапы.
    /// </summary>
    /// <param name="activity">Этап.</param>
    /// <returns>Подчиненные этапы.</returns>
    [Remote]
    public static IQueryable<IProjectActivity> GetChildActivities(IProjectActivity activity)
    {
      return ProjectActivities.GetAll().Where(c => ProjectPlans.Equals(c.ProjectPlan, activity.ProjectPlan) && ProjectActivities.Equals(activity, c.LeadingActivity));
    }

    /// <summary>
    /// Генерирует полный номер этапа.
    /// </summary>
    /// <returns>Полный номер этапа.</returns>
    [Remote]
    public string GenerateFullNumber()
    {
      return _obj.LeadingActivity != null ? string.Format("{0}.{1}", _obj.LeadingActivity.FullNumber, _obj.Number.Value) : _obj.Number.Value.ToString();
    }

    /// <summary>
    /// Возвращает новый номер этапа.
    /// </summary>
    /// <returns>Новый номер этапа.</returns>
    [Remote]
    public int GetNewNumber()
    {
      int? maxNumber = null;
      if (_obj.LeadingActivity == null)
        maxNumber = GetActivities(_obj.ProjectPlan).Where(c => c.LeadingActivity == null).Max(c => c.Number);
      else
        maxNumber = GetChildActivities(_obj.LeadingActivity).Max(c => c.Number);
      
      return maxNumber.HasValue ? maxNumber.Value + 1 : 1;
    }
    
    /// <summary>
    /// Признак того, что у этапа существуют подчиненные этапы.
    /// </summary>
    /// <returns>True, если у этапа существуют подчиненные этапы.</returns>
    [Remote]
    public bool ExistChildActivity()
    {
      return GetChildActivities(_obj).Any();
    }

  }
}