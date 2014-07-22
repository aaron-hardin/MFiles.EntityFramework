// This file comes from Microsoft's Entity Framework and is covered under their license.
// See LICENSE.txt

using System;
using EnvDTE;
using MFiles.EntityFramework.PowerShell.Helper;

namespace MFiles.EntityFramework.PowerShell
{
	internal abstract class MigrationsDomainCommand
	{

		private readonly AppDomain _domain;
		private readonly DomainDispatcher _dispatcher;

		public virtual Project Project
		{
			get { return (Project) _domain.GetData("project"); }
		}

		public Project StartUpProject
		{
			get { return (Project) _domain.GetData("startUpProject"); }
		}

		public Project ContextProject
		{
			get { return (Project) _domain.GetData("contextProject"); }
		}

		public string ContextAssemblyName
		{
			get { return (string) _domain.GetData("contextAssemblyName"); }
		}

		public string AppDomainBaseDirectory
		{
			get { return (string) _domain.GetData("appDomainBaseDirectory"); }
		}

		protected AppDomain Domain
		{
			get { return _domain; }
		}

		protected MigrationsDomainCommand()
		{
			_domain = AppDomain.CurrentDomain;
			_dispatcher = (DomainDispatcher) _domain.GetData("efDispatcher");
		}

		public void Execute(Action command)
		{
			Init();
			try
			{
				command();
			}
			catch (Exception ex)
			{
				Throw(ex);
			}
		}

		public virtual void WriteLine(string message)
		{
			_dispatcher.WriteLine(message);
		}

		public virtual void WriteWarning(string message)
		{
			_dispatcher.WriteWarning(message);
		}

		public void WriteVerbose(string message)
		{
			_dispatcher.WriteVerbose(message);
		}

		public T GetAnonymousArgument<T>(string name)
		{
			return (T) _domain.GetData(name);
		}

		private void Init()
		{
			_domain.SetData("wasError", false);
			_domain.SetData("error.Message", null);
			_domain.SetData("error.TypeName", null);
			_domain.SetData("error.StackTrace", null);
		}

		private void Throw(Exception ex)
		{
			_domain.SetData("wasError", true);
			_domain.SetData("error.Message", ex.Message);
			_domain.SetData("error.TypeName", ex.GetType().FullName);
			_domain.SetData("error.StackTrace", ex.ToString());
		}
	}
}
