using System;
using System.Collections.Generic;
using System.Linq;
using Sungero.Core;
using Sungero.CoreEntities;
using DirRX.ProjectPlanner.ProjectPlan;

namespace DirRX.ProjectPlanner.Client
{
	partial class ProjectPlanFunctions
	{
		/// <summary>
		/// Заблокировать план проекта перед его открытием.
		/// </summary>
		/// <param name="projectPlan">План проекта</param>
		/// <returns>Строка с сообщением о блокировке. Возвращается пустая строка, если блокировок нет.</returns>
		[Public]
		public static string SetLockOnProjectPlan(IProjectPlan projectPlan)
		{
			if (projectPlan.AccessRights.CanUpdate())
			{
				var lockinfoProjectPlan = Locks.GetLockInfo(projectPlan);
				var relatedProject = Functions.ProjectPlan.Remote.GetLinkedProejct(projectPlan);
				
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
	}
}