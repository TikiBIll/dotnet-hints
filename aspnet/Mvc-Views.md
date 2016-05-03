ASP.NET Mvc 6
=============

Controller Source Code w/documentation:
https://github.com/aspnet/Mvc/blob/dev/src/Microsoft.AspNetCore.Mvc.ViewFeatures/Controller.cs

Changing the folder for views (aka feature folder structure):
https://scottsauber.com/2016/04/25/feature-folder-structure-in-asp-net-core/


By default, the view name will match the method name:
```c#
public IActionResult Index()
{   
  return View(); //Will use Index.cshtml
}
```

An alternative name can be specified, no extension:
```c#
public IActionResult Index()
{   
  return View("AltIndex"); //Will use AltIndex.cshtml
}
```

A model can be specified:
```c#
public IActionResult Index()
{   
  return View(model: someModel); //Equivalent to ViewData.Model = someModel;
}
```

Do not try to use an anonymous type (http://stackoverflow.com/questions/1178984/dynamic-typed-viewpage):
```c#
public IActionResult Index()
{   
  var customModel = new { Name = "Bill", Age = "TimeLess" };  
  return View( model: customModel ); //Will throw an exception when the view accesses @ViewData.Model.Name
}
```

If one must:
```c#
public IActionResult Index()
{   
  var customModel = new Dictionary<string,string>(){ ["Name"] = "Bill", ["Age"] = "TimeLess" };
  return View( model: customModel ); //Ok, @ViewData.Model["Name"]
}
```
