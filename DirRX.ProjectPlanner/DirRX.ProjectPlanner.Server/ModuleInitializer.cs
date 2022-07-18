using System;
using System.Collections.Generic;
using System.Linq;
using Sungero.Core;
using Sungero.CoreEntities;
using Sungero.Domain.Initialization;

namespace DirRX.ProjectPlanner.Server
{
  public partial class ModuleInitializer
  {

    public override void Initializing(Sungero.Domain.ModuleInitializingEventArgs e)
    {
      Init();
    }
    
    [Public]
    public static void Init()
    {
      GrantRightsOnSpecialFolders();
      GrantRightsOnActivityTasks();
      GrantRightsOnProjectActivity();
      GrantRightsOnProjectPlans();
      GrantRightsOnProjectResources();
      GrantRightsOnProjectResourceTypes();  
      CreateAssociatedApplication();
      CreateDocumentType();
      CreateDocumentKinds();
      CreateResourceTypes();
      GrantRightsOnOpensProjectPlansFromCards();
      GrantRightsOnProjectActivityToProjectManager();
      ConvertObsoletePlans();
      CreateResourceLinksIfNotExists();
    }
    
    #region Выдача прав на объекты системы.
    public static void GrantRightsOnSpecialFolders()
    {
      try
      {
        var allUsers = Roles.AllUsers;
        if (!SpecialFolders.IncomeProjectsExecution.AccessRights.CanRead(allUsers))
        {
          SpecialFolders.IncomeProjectsExecution.AccessRights.Grant(allUsers, DefaultAccessRightsTypes.Read);
          SpecialFolders.IncomeProjectsExecution.AccessRights.Save();
        }
        
        if (!SpecialFolders.ProjectsExecution.AccessRights.CanRead(allUsers))
        {
          SpecialFolders.ProjectsExecution.AccessRights.Grant(allUsers, DefaultAccessRightsTypes.Read);
          SpecialFolders.ProjectsExecution.AccessRights.Save();
        }
        
        InitializationLogger.Debug("Выданы права на вычисляемую папку 'Исполнение проектов'");
      }
      catch (Sungero.Domain.Shared.Exceptions.SessionException ex)
      {
        InitializationLogger.DebugFormat("Не удалось выдать права доступа к типу документа 'Исполнение проектов' для всех пользователей системы. Возможно права уже настраивались ранее вручную, вы можете изменить их в панели 'Администрирование' самостоятельно. {0}", ex);
      }
      
    }
    
    public static void GrantRightsOnActivityTasks()
    {
      try
      {
        if (!ProjectPlanner.ProjectActivityTasks.AccessRights.CanManageOrDelegate(Roles.AllUsers))
        {
          ProjectPlanner.ProjectActivityTasks.AccessRights.Grant(Roles.AllUsers, DefaultAccessRightsTypes.FullAccess);
          ProjectPlanner.ProjectActivityTasks.AccessRights.Save();
        }
      }
      catch (Sungero.Domain.Shared.Exceptions.SessionException ex)
      {
        InitializationLogger.DebugFormat("Не удалось выдать права доступа к типу документа 'Задача по этапу' для всех пользователей системы. Возможно права уже настраивались ранее вручную, вы можете изменить их в панели 'Администрирование' самостоятельно. {0}", ex);
      }
    }
    
    public static void GrantRightsOnOpensProjectPlansFromCards()
    {
      try
      {
        InitializationLogger.Debug("Init: Grant rights on Opens project Plans from Card.");
        if (!ProjectPlanner.OpensProjectPlansFromCards.AccessRights.CanManageOrDelegate(Roles.AllUsers))
        {
          ProjectPlanner.OpensProjectPlansFromCards.AccessRights.Grant(Roles.AllUsers, DefaultAccessRightsTypes.FullAccess);
          ProjectPlanner.OpensProjectPlansFromCards.AccessRights.Save();
        }
      }
      catch (Sungero.Domain.Shared.Exceptions.SessionException ex)
      {
        InitializationLogger.DebugFormat("Не удалось выдать права доступа к типу документа 'Открытые из карточек планы проектов' для всех пользователей системы. Возможно права уже настраивались ранее вручную, вы можете изменить их в панели 'Администрирование' самостоятельно. {0}", ex);
      }
    }
    
    public static void GrantRightsOnProjectPlans()
    {
      try
      {
        InitializationLogger.Debug("Init: Grant rights on project plans.");
        if (!ProjectPlanner.ProjectPlanRXes.AccessRights.CanCreate(Roles.AllUsers))
        {
          ProjectPlanner.ProjectPlanRXes.AccessRights.Grant(Roles.AllUsers, DefaultAccessRightsTypes.Create);
          ProjectPlanner.ProjectPlanRXes.AccessRights.Save();
        }
      }
      catch (Sungero.Domain.Shared.Exceptions.SessionException ex)
      {
        InitializationLogger.DebugFormat("Не удалось выдать права доступа к типу документа 'Планы проектов' для всех пользователей системы. Возможно права уже настраивались ранее вручную, вы можете изменить их в панели 'Администрирование' самостоятельно. {0}", ex);
      }
    }
    
    public static void GrantRightsOnProjectActivity()
    {
      try
      {
        InitializationLogger.Debug("Init: Grant rights on project activity.");
        if (!ProjectPlanner.ProjectActivities.AccessRights.CanCreate(Roles.AllUsers))
        {
          ProjectPlanner.ProjectActivities.AccessRights.Grant(Roles.AllUsers, DefaultAccessRightsTypes.Create);
          ProjectPlanner.ProjectActivities.AccessRights.Save();
        }
      }
      catch (Sungero.Domain.Shared.Exceptions.SessionException ex)
      {
        InitializationLogger.DebugFormat("Не удалось выдать права доступа к типу документа 'Этап плана' для всех пользователей системы. Возможно права уже настраивались ранее вручную, вы можете изменить их в панели 'Администрирование' самостоятельно. {0}", ex);
      }
    }
    
    public static void GrantRightsOnProjectResources()
    {
      try
      {
        InitializationLogger.Debug("Init: Grant rights on project resources.");
        if (!ProjectPlanner.ProjectsResources.AccessRights.CanCreate(Roles.AllUsers))
        {
          ProjectPlanner.ProjectsResources.AccessRights.Grant(Roles.AllUsers, DefaultAccessRightsTypes.Create);
          ProjectPlanner.ProjectsResources.AccessRights.Save();
        }
      }
      catch (Sungero.Domain.Shared.Exceptions.SessionException ex)
      {
        InitializationLogger.DebugFormat("Не удалось выдать права доступа к типу документа 'Ресурсы плана' для всех пользователей системы. Возможно права уже настраивались ранее вручную, вы можете изменить их в панели 'Администрирование' самостоятельно. {0}", ex);
      }
      
    }
    
    public static void GrantRightsOnProjectResourceTypes()
    {
      try
      {
        InitializationLogger.Debug("Init: Grant rights on project resource types.");
        if (!ProjectPlanner.ProjectResourceTypes.AccessRights.CanRead(Roles.AllUsers))
        {
          ProjectPlanner.ProjectResourceTypes.AccessRights.Grant(Roles.AllUsers, DefaultAccessRightsTypes.Read);
          ProjectPlanner.ProjectResourceTypes.AccessRights.Save();
        }
      }
      catch (Sungero.Domain.Shared.Exceptions.SessionException ex)
      {
        InitializationLogger.DebugFormat("Не удалось выдать права доступа к типу документа 'Типы ресурсов' для всех пользователей системы. Возможно права уже настраивались ранее вручную, вы можете изменить их в панели 'Администрирование' самостоятельно. {0}", ex);
      }
    }
    
    public static void GrantRightsOnProjectActivityToProjectManager()
    {
      try
      {
        InitializationLogger.Debug("Init: Grant rights on project activity to Project Manager.");
        var role = Sungero.Docflow.PublicInitializationFunctions.Module.GetProjectManagersRole();
        if (!ProjectPlanner.ProjectActivities.AccessRights.CanManageOrDelegate(role))
        {
          ProjectPlanner.ProjectActivities.AccessRights.Grant(role, DefaultAccessRightsTypes.FullAccess);
          ProjectPlanner.ProjectActivities.AccessRights.Save();
        }
      }
      catch (Sungero.Domain.Shared.Exceptions.SessionException ex)
      {
        InitializationLogger.DebugFormat("Не удалось выдать права доступа к типу документа 'Этап плана' для менеджера проектов. Возможно права уже настраивались ранее вручную, вы можете изменить их в панели 'Администрирование' самостоятельно. {0}", ex);
      }
    }
    #endregion
    
    public static void CreateResourceLinksIfNotExists()
    {
      using (var command = SQL.GetCurrentConnection().CreateCommand())
      {
        InitializationLogger.Debug("Execute create (if not exists) resourcelinks query");
        command.CommandText = Queries.Module.CreateResourceLinksIfNotExist;
        command.ExecuteNonQuery();
      }
    }
    
    /// <summary>
    /// Преобразование старых планов проектов в новые
    /// </summary>
    public static void ConvertObsoletePlans() 
    {
      InitializationLogger.Debug("Start async convert obsolete plans query");
      var handler = new ProjectPlanner.AsyncHandlers.ConvertPlanAsync();
      handler.ExecuteAsync();
    }
    
    /// <summary>
    /// Создание типа документа.
    /// </summary>
    public static void CreateDocumentType()
    {
      InitializationLogger.Debug("Init: Create document type");
      
      Sungero.Docflow.PublicInitializationFunctions.Module.CreateDocumentType(DirRX.ProjectPlanner.Resources.ProjectPlanName, ProjectPlanRX.ClassTypeGuid, Sungero.Docflow.DocumentType.DocumentFlow.Inner, true);
    }
    
    /// <summary>
    /// Создание вида документа.
    /// </summary>
    public static void CreateDocumentKinds()
    {
      InitializationLogger.Debug("Init: Create document kinds.");
      
      var notNumerable = Sungero.Docflow.DocumentKind.NumberingType.NotNumerable;
      var autoFormattedName = false;
      
      Sungero.Docflow.PublicInitializationFunctions.Module.CreateDocumentKind(DirRX.ProjectPlanner.Resources.ProjectPlanName,
                                                                              DirRX.ProjectPlanner.Resources.ProjectPlanName, notNumerable,
                                                                              Sungero.Docflow.DocumentKind.DocumentFlow.Inner, autoFormattedName, false, ProjectPlanRX.ClassTypeGuid, null,
                                                                              Constants.Module.ProjectPlanDocKindGuid, true);
    }
    
    /// <summary>
    /// Задание приложения-обработчика для расширения .rxpp.
    /// </summary>
    public static void CreateAssociatedApplication()
    {
      InitializationLogger.Debug("Init: Create associated application.");
      if (!Sungero.Content.AssociatedApplications.GetAll(x => x.Extension == "rxpp").Any())
      {
        var app = Sungero.Content.AssociatedApplications.Create();
        app.Extension = "rxpp";
        app.Name = DirRX.ProjectPlanner.Resources.AplicationName;
        app.MonitoringType = Sungero.Content.AssociatedApplication.MonitoringType.ByProcessAndWindow;
        var fileType = Sungero.Content.FilesTypes.Create();
        fileType.Name = DirRX.ProjectPlanner.Resources.ProjectsFileTypeName;
        app.FilesType = fileType;
        app.Save();
      }
    }
    
    /// <summary>
    /// Создание типов ресурсов.
    /// </summary>
    public static void CreateResourceTypes()
    {
      if (!ProjectResourceTypes.GetAll(x => x.ServiceName == Constants.Module.ResourceTypes.Users).Any())
      {
        var resourceType = ProjectResourceTypes.Create();
        resourceType.Name = DirRX.ProjectPlanner.Resources.EmployeeResourceTypeName;
        resourceType.MeasureUnit = DirRX.ProjectPlanner.Resources.EmployeeResourceTypeUnit;
        resourceType.ServiceName = Constants.Module.ResourceTypes.Users;
        resourceType.Save();
      }
      
      if (!ProjectResourceTypes.GetAll(x => x.ServiceName == Constants.Module.ResourceTypes.MaterialResources).Any())
      {
        var resourceType = ProjectResourceTypes.Create();
        resourceType.Name = DirRX.ProjectPlanner.Resources.MaterialResourcesTypeName;
        resourceType.MeasureUnit = DirRX.ProjectPlanner.Resources.EmployeeResourceTypeUnit;
        resourceType.ServiceName = Constants.Module.ResourceTypes.MaterialResources;
        resourceType.Save();
      }     
    }
    
  }
}
