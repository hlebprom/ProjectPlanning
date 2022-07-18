using System;
using System.Collections.Generic;
using System.Linq;
using Sungero.Core;
using Sungero.CoreEntities;

namespace DirRX.ProjectPlanner.Server
{
  public class ModuleAsyncHandlers
  {

    public virtual void GeneratePlanBody(DirRX.ProjectPlanner.Server.AsyncHandlerInvokeArgs.GeneratePlanBodyInvokeArgs args)
    {
      if (args.RetryIteration > 20)
      {
        args.Retry = false;
        return;
      }
      Logger.Debug("Init: rebuild project models.");
      foreach(var projectPlan in ProjectPlanRXes.GetAll(p => !p.BodyConverted.HasValue || p.BodyConverted == false))
      {
        var planLockInfo = Locks.GetLockInfo(projectPlan);
			  if (projectPlan.LastVersion != null && planLockInfo != null && !planLockInfo.IsLockedByOther)
			  {
			    try
			    {
			      Transactions.Execute(() => {
			      foreach (var version in projectPlan.Versions) 
			      {
			        if (Locks.GetLockInfo(version.Body).IsLockedByOther || Locks.GetLockInfo(version.PublicBody).IsLockedByOther)
			        {
			          Logger.DebugFormat("Тело версии {0} плана проекта {1} заблокировано другим пользователем системы. перегенерация тела пропущена.", version.Number.ToString(), projectPlan.Name);
			          args.Retry = true;
			          continue;
			        }
			        
			        var versionApproved = Signatures.Get(version)
			          .Where(s => s.SignCertificate != null).Where(s => s.SignatureType == SignatureType.Approval).Any();
			           DirRX.ProjectPlanner.PublicFunctions.Module.Remote.WriteJsonBodyToProjectVersion(projectPlan, version.Number.Value, versionApproved);
		          
			      }
	         projectPlan.BodyConverted = true;
			     projectPlan.Save();
			     });
	         Logger.DebugFormat("Конвертация плана проекта {0} успешна.", projectPlan.Name);
			    }
			    catch(Exception ex)
			    {
			      Logger.ErrorFormat("Перегенерация плана проекта Id = {0} завершилась с ошибкой: {1}{2}{3}", projectPlan.Id, ex.Message, Environment.NewLine, ex.StackTrace);
	          args.Retry = true;
	          continue;
			    }
			  }
			  else if (planLockInfo == null || planLockInfo.IsLockedByOther)
			  {
			     args.Retry = true;
			     Logger.DebugFormat("Конвертация плана проекта {0} пропущена из-за блокировки.", projectPlan.Name);
			  }
      }
      
      var notConvertedPlans = ProjectPlanRXes.GetAll(p => !p.BodyConverted.HasValue || p.BodyConverted == false).Select(p => p.Name).ToArray();
      
      if (notConvertedPlans.Any())
      {
        Logger.DebugFormat("Конвертация планов проекта завершена. Не сконвертированы планы: {0}", string.Join("; ", notConvertedPlans));
      }
      else
      {
        Logger.Debug("Конвертация планов проекта завершена. Все планы успешно сконвертированы.");
      }
      
      
      
    }

    public virtual void ConvertPlanAsync(DirRX.ProjectPlanner.Server.AsyncHandlerInvokeArgs.ConvertPlanAsyncInvokeArgs args)
    {
      if (args.RetryIteration > 30)
      {
        args.Retry = false;
        StartRebuildBody();
        return;
      }
      
      var olds = ProjectPlanObsoletes.GetAll().ToList();
      var needsToRebuild = ProjectPlanRXes.GetAll(p => !p.BodyConverted.HasValue || p.BodyConverted == false);
      if (olds.Count == 0 && needsToRebuild.Any())
      {
        StartRebuildBody();
        args.Retry = false;
        return;
      }
      
      try 
      {
        foreach (var oldPlan in olds)
        {
          var lockPlanInfo = Locks.GetLockInfo(oldPlan);
          if(lockPlanInfo.IsLockedByOther)
          {
            Logger.DebugFormat(DirRX.ProjectPlanner.Resources.CannotConvertFormat(oldPlan.Id, lockPlanInfo.OwnerName));
            args.Retry = true;
            continue;
          }
          else
          {
            try 
            {
              Transactions.Execute( () => 
                                   {
              var edoc = ProjectPlanObsoletes.Get(oldPlan.Id);
              var convertedPlan = edoc.ConvertTo(ProjectPlanRXes.Info);
              convertedPlan.Save();
              
              var newPlan = ProjectPlanRXes.Get(edoc.Id);
              newPlan.DocumentKind = Sungero.Docflow.DocumentKinds.GetAll(x => x.DocumentType.DocumentTypeGuid == Server.ProjectPlanRX.ClassTypeGuid.ToString()).FirstOrDefault();
              newPlan.BodyConverted = false;
              newPlan.Save();
                                   });
              
            }
            catch (Exception ex) 
            {
              Logger.DebugFormat(DirRX.ProjectPlanner.Resources.CouldNotConvertProjectPlanFormat(oldPlan.Id), ex.ToString());
              args.Retry = true;
            }
          }
        }
        
        using (var command = SQL.GetCurrentConnection().CreateCommand())
        {
          Logger.Debug("Execute restore plan links query");
          command.CommandText = Queries.Module.RestorePlanLinks;
          command.ExecuteNonQuery();
        }
        
        if (!args.PlansGenerated) 
        {
          StartRebuildBody();
        }
        
      }
      catch(Exception ex) 
      {
        Logger.Error(ex + "Произошла ошибка при конвертации планов проекта");
        args.Retry = true;
      }
      finally
      {
        StartRebuildBody();
      }
      args.RetryIteration++;
    }
    
    private static void StartRebuildBody() 
    {
      var handler = new ProjectPlanner.AsyncHandlers.GeneratePlanBody();
      handler.ExecuteAsync();
    }

    public virtual void GrantRightsActivityResponsible(DirRX.ProjectPlanner.Server.AsyncHandlerInvokeArgs.GrantRightsActivityResponsibleInvokeArgs args)
    {
      if (args.RetryIteration > 10)
      {
        args.Retry = false;
        return;
      }

      var projectPlan = ProjectPlanRXes.Get(args.ProjectPlanId);
      var project = args.ProjectId != 0 ? ProjectPlanning.Projects.Get(args.ProjectId) : null;
      var employee = Sungero.Company.Employees.Get(args.EmployeeId);
      
      var lockPlanInfo = Locks.GetLockInfo(projectPlan);
      if(lockPlanInfo.IsLockedByOther)
      {
        Logger.DebugFormat(DirRX.ProjectPlanner.Resources.ProjectPlanLockedError, employee.Name, projectPlan.Id.ToString(), lockPlanInfo.OwnerName);
        args.Retry = true;
        args.RetryIteration++;
        return;
      }
      else
      {
        if (!projectPlan.AccessRights.CanRead(employee))
        {
          projectPlan.AccessRights.Grant(employee, DefaultAccessRightsTypes.Read);
          projectPlan.AccessRights.Save();
        }
      }
      
      if(project != null)
      {
        var lockProjectInfo = Locks.GetLockInfo(project);
        if(lockProjectInfo.IsLockedByOther)
        {
          Logger.DebugFormat(DirRX.ProjectPlanner.Resources.ProjectLockedError, employee.Name, project.Id.ToString(), lockProjectInfo.OwnerName);
          args.Retry = true;
          args.RetryIteration++;
          return;
        }
        else
        {
          if (project != null && !project.AccessRights.CanRead(employee))
          {
            project.AccessRights.Grant(employee, DefaultAccessRightsTypes.Read);
            project.AccessRights.Save();
          }
        }
      }

      args.Retry = false;
    }
  }
}