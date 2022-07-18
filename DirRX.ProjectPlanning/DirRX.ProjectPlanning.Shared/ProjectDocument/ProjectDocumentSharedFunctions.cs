﻿using System;
using System.Collections.Generic;
using System.Linq;
using Sungero.Core;
using Sungero.CoreEntities;
using DirRX.ProjectPlanning.ProjectDocument;

namespace DirRX.ProjectPlanning.Shared
{
  partial class ProjectDocumentFunctions
  {
  public override bool IsProjectDocument()
  {
    return _obj.Project != null && _obj.DocumentKind.ProjectsAccounting.Value;
  }
  }
}