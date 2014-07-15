using System;
using System.CodeDom;
using System.CodeDom.Compiler;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using EnvDTE;
using MFiles.EntityFramework.PowerShell.Extensions;
using MFilesAPI;
using CodeNamespace = System.CodeDom.CodeNamespace;

namespace MFiles.EntityFramework.PowerShell.Utilities
{
	internal class ObjectClassGenerator
	{
		private readonly Project _project;
		private readonly ObjType _objType;
		private readonly ObjectClass _objectClass;
		private readonly Vault _vault;
		private readonly MigrationsDomainCommand _command;

		public ObjectClassGenerator(ObjectClass objectClass, ObjType objType, Project project, Vault vault, MigrationsDomainCommand command)
		{
			_objectClass = objectClass;
			_objType = objType;
			_project = project;
			_vault = vault;
			_command = command;
		}

		public bool Exists
		{
			get
			{
				string absolutePath = Path.Combine(_project.GetProjectDir(), FilePath);
				return File.Exists(absolutePath);
			}
		}

		public string FilePath
		{
			get { return Path.Combine("Models", _objectClass.Name.CleanName() + ".cs"); }
		}

		public string GenerateClassCode()
		{
			CodeTypeDeclaration targetClass = new CodeTypeDeclaration(_objectClass.Name.CleanName())
			{
				IsClass = true,
				TypeAttributes = TypeAttributes.Public
			};
			targetClass.BaseTypes.Add("OT_"+_objType.NameSingular.CleanName());

			// Declare a new generated code attribute
			CodeAttributeDeclaration codeAttrDecl = new CodeAttributeDeclaration("MetaStructureClass");
			targetClass.CustomAttributes.Add(codeAttrDecl);

			AddProperties(targetClass);

			CodeNamespace targetNamespace = new CodeNamespace(_project.GetModelNamespace());
			targetNamespace.Types.Add(targetClass);

			StringBuilder sbCode = new StringBuilder();
			using (StringWriter sw = new StringWriter(sbCode))
			{
				CodeDomProvider provider = CodeDomProvider.CreateProvider("CSharp");
				CodeGeneratorOptions options = new CodeGeneratorOptions { BracingStyle = "C" };

				provider.GenerateCodeFromNamespace(targetNamespace, sw, options);
			}

			string[] namespaces = { "System", "MFilesAPI", "testpkg.Design", "System.Collections.Generic" };
			string usingSt = namespaces.Aggregate("", (current, ns) => current + string.Format("using {0};\r\n", ns));

			string code = string.Format("{0}\r\n{1}", usingSt, sbCode);

			return code;
		}

		/// <summary> 
		/// Add properties to the class. 
		/// </summary> 
		private void AddProperties(CodeTypeDeclaration targetClass)
		{
			foreach (AssociatedPropertyDef associatedPropertyDef in _objectClass.AssociatedPropertyDefs)
			{
				PropertyDef pdef = _vault.PropertyDefOperations.GetPropertyDef(associatedPropertyDef.PropertyDef);

				// Skip Automatic Values.
				// TODO: handle certain ones?
				if(pdef.AutomaticValueType != MFAutomaticValueType.MFAutomaticValueTypeNone)
					continue;

				// Ignore the built in properties, those should be handled on a lower level (and for specific ones only).
				if(pdef.ID < 101 && pdef.ID > 0)
					continue;

				CodeMemberProperty property = new CodeMemberProperty
				{
					Attributes = MemberAttributes.Public | MemberAttributes.Final,
					Name = pdef.Name.CleanName()
				};
				property.Comments.Add(new CodeCommentStatement(string.Format("Binding property for {0}.", pdef.Name)));

				switch (pdef.DataType)
				{
					case MFDataType.MFDatatypeMultiLineText:
					case MFDataType.MFDatatypeText:
						property.Type = new CodeTypeReference(typeof (string));
						property.GetStatements.Add(new CodeMethodReturnStatement(
							new CodeFieldReferenceExpression(
								new CodeThisReferenceExpression(), "GetProperty()")));
						property.SetStatements.Add(new CodeMethodInvokeExpression(new CodeThisReferenceExpression(), "SetProperty",
							new CodePropertySetValueReferenceExpression()));
						break;
					case MFDataType.MFDatatypeDate:
						property.Type = new CodeTypeReference(typeof (DateTime));
						property.GetStatements.Add(new CodeMethodReturnStatement(
							new CodeFieldReferenceExpression(
								new CodeThisReferenceExpression(), "GetProperty()")));
						property.SetStatements.Add(new CodeMethodInvokeExpression(new CodeThisReferenceExpression(), "SetProperty",
							new CodePropertySetValueReferenceExpression()));
						break;
					case MFDataType.MFDatatypeBoolean:
						property.Type = new CodeTypeReference(typeof(bool));
						property.GetStatements.Add(new CodeMethodReturnStatement(
							new CodeFieldReferenceExpression(
								new CodeThisReferenceExpression(), "GetProperty()")));
						property.SetStatements.Add(new CodeMethodInvokeExpression(new CodeThisReferenceExpression(), "SetProperty",
							new CodePropertySetValueReferenceExpression()));
						break;
					case MFDataType.MFDatatypeInteger:
						property.Type = new CodeTypeReference(typeof(int));
						property.GetStatements.Add(new CodeMethodReturnStatement(
							new CodeFieldReferenceExpression(
								new CodeThisReferenceExpression(), "GetProperty()")));
						property.SetStatements.Add(new CodeMethodInvokeExpression(new CodeThisReferenceExpression(), "SetProperty",
							new CodePropertySetValueReferenceExpression()));
						break;
					case MFDataType.MFDatatypeFloating:
						property.Type = new CodeTypeReference(typeof(float));
						property.GetStatements.Add(new CodeMethodReturnStatement(
							new CodeFieldReferenceExpression(
								new CodeThisReferenceExpression(), "GetProperty()")));
						property.SetStatements.Add(new CodeMethodInvokeExpression(new CodeThisReferenceExpression(), "SetProperty",
							new CodePropertySetValueReferenceExpression()));
						break;
					case MFDataType.MFDatatypeLookup:
						ObjType objType = _vault.ObjectTypeOperations.GetObjectType(pdef.ValueList);
						if (!objType.RealObjectType)
						{
							//_command.WriteWarning("Value lists not yet supported.");
							//continue;
							property.Type = new CodeTypeReference(objType.NameSingular.CleanName());
						}
						else
						{
							property.Type = new CodeTypeReference("OT_" + objType.NameSingular.CleanName());	
						}
						property.GetStatements.Add(new CodeMethodReturnStatement(
							new CodeFieldReferenceExpression(
								new CodeThisReferenceExpression(), "GetProperty()")));
						property.SetStatements.Add(new CodeMethodInvokeExpression(new CodeThisReferenceExpression(), "SetProperty",
							new CodePropertySetValueReferenceExpression()));
						break;
					case MFDataType.MFDatatypeMultiSelectLookup:
						ObjType objTypeMulti = _vault.ObjectTypeOperations.GetObjectType(pdef.ValueList);
						if (!objTypeMulti.RealObjectType)
						{
							//_command.WriteWarning("Value lists not yet supported.");
							//continue;
							property.Type = new CodeTypeReference("List<"+objTypeMulti.NameSingular.CleanName()+">");
						}
						else
						{
							property.Type = new CodeTypeReference("List<OT_" + objTypeMulti.NameSingular.CleanName() + ">");	
						}
						property.GetStatements.Add(new CodeMethodReturnStatement(
							new CodeFieldReferenceExpression(
								new CodeThisReferenceExpression(), "GetProperty()")));
						property.SetStatements.Add(new CodeMethodInvokeExpression(new CodeThisReferenceExpression(), "SetProperty",
							new CodePropertySetValueReferenceExpression()));
						break;
					default:
						//throw new NotImplementedException(string.Format("Generation of datatype {0} not yet supported.", pdef.DataType));
						_command.WriteWarning(string.Format("Generation of datatype {0} not yet supported.", pdef.DataType));
						break;
				}

				targetClass.Members.Add(property);
			}
		}
	}
}
