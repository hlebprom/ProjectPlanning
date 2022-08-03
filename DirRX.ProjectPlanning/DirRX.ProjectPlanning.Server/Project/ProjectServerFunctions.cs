using System;
using System.Collections.Generic;
using System.Linq;
using Sungero.Core;
using Sungero.CoreEntities;
using DirRX.ProjectPlanning.Project;

namespace DirRX.ProjectPlanning.Server
{
	partial class ProjectFunctions
	{
		/// <summary>
		/// Возвращает информацию о блокировке проекта.
		/// </summary>
		/// <returns>Информация о блокировке проекта.</returns>
		/// <remarks>
		/// Если проект заблокирован, то возвращается информация о блокировке.
		/// Если же не заблокирован, то пустая строка.
		/// </remarks>
		[Remote]
		public string GetLockInfo()
		{
			var lockInfo = Locks.GetLockInfo(_obj);
			return lockInfo.IsLockedByOther && !string.IsNullOrEmpty(lockInfo.LockedMessage) ? lockInfo.LockedMessage.ToString() : string.Empty;
		}
		
	}
}