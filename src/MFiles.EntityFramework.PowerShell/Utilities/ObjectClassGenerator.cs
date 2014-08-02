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

		public string PartialsFilePath
		{
			get { return Path.Combine("Models\\Partials", _objectClass.Name.CleanName() + ".cs"); }
		}

		public string GenerateClassCode(bool emptyPartial = false)
		{
			CodeTypeDeclaration targetClass = new CodeTypeDeclaration(_objectClass.Name.CleanName())
			{
				IsClass = true,
				IsPartial = true,
				TypeAttributes = TypeAttributes.Public
			};
			targetClass.BaseTypes.Add("OT_"+_objType.NameSingular.CleanName());

			if (!emptyPartial)
			{
				// Declare a new generated code attribute
				CodeAttributeDeclaration codeAttrDecl = new CodeAttributeDeclaration("MetaStructureClass");
				CodeAttributeArgument argument = new CodeAttributeArgument("Name", new CodeArgumentReferenceExpression(_objectClass.Name));
				codeAttrDecl.Arguments.Add(argument);
				targetClass.CustomAttributes.Add(codeAttrDecl);

				AddProperties(targetClass);
				Common.AddConstructors(targetClass);
			}
			CodeNamespace targetNamespace = new CodeNamespace(_project.GetModelNamespace());
			targetNamespace.Types.Add(targetClass);
			
			StringBuilder sbCode = new StringBuilder();
			using (StringWriter sw = new StringWriter(sbCode))
			{
				CodeDomProvider provider = CodeDomProvider.CreateProvider("CSharp");
				CodeGeneratorOptions options = new CodeGeneratorOptions { BracingStyle = "C" };

				provider.GenerateCodeFromNamespace(targetNamespace, sw, options);
			}

			string[] namespaces = { "System", "MFilesAPI", "MFiles.EntityFramework.Design", "System.Collections.Generic" };
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
				PropertyDefAdmin pdefAdmin = _vault.PropertyDefOperations.GetPropertyDefAdmin(associatedPropertyDef.PropertyDef);

				// Skip Automatic Values.
				// TODO: handle certain ones?
				if(pdefAdmin.PropertyDef.AutomaticValueType != MFAutomaticValueType.MFAutomaticValueTypeNone)
					continue;

				// Ignore the built in properties, those should be handled on a lower level (and for specific ones only).
				if(pdefAdmin.PropertyDef.ID < 101 && pdefAdmin.PropertyDef.ID > 0)
					continue;

				CodeMemberProperty property = new CodeMemberProperty
				{
					Attributes = MemberAttributes.Public | MemberAttributes.Final,
					Name = pdefAdmin.PropertyDef.Name.CleanName()
				};
				property.Comments.Add(new CodeCommentStatement(string.Format("Binding property for {0}.", pdefAdmin.PropertyDef.Name)));

				string getParam = pdefAdmin.PropertyDef.GUID;
				if (!string.IsNullOrWhiteSpace(pdefAdmin.SemanticAliases.Value))
					getParam = pdefAdmin.SemanticAliases.Value.Split(';')[0];
				getParam = "\"" + getParam + "\"";

				switch (pdefAdmin.PropertyDef.DataType)
				{
					case MFDataType.MFDatatypeMultiLineText:
					case MFDataType.MFDatatypeText:
						property.Type = new CodeTypeReference(typeof (string));
						property.GetStatements.Add(new CodeMethodReturnStatement(
							new CodeFieldReferenceExpression(
								new CodeThisReferenceExpression(), string.Format("GetPropertyText({0})", getParam))));
						property.SetStatements.Add(new CodeMethodInvokeExpression(new CodeThisReferenceExpression(), "SetProperty",
							new CodeDirectionExpression(FieldDirection.In, new CodeArgumentReferenceExpression(getParam)),
							new CodeDirectionExpression(FieldDirection.In, new CodeArgumentReferenceExpression("MFDataType." + pdefAdmin.PropertyDef.DataType)),
							new CodePropertySetValueReferenceExpression()));
						break;
					case MFDataType.MFDatatypeDate:
						property.Type = new CodeTypeReference(typeof (DateTime));
						property.GetStatements.Add(new CodeMethodReturnStatement(
							new CodeFieldReferenceExpression(
								new CodeThisReferenceExpression(), string.Format("GetProperty({0}).Value.Value", getParam))));
						property.SetStatements.Add(new CodeMethodInvokeExpression(new CodeThisReferenceExpression(), "SetProperty",
							new CodeDirectionExpression(FieldDirection.In, new CodeArgumentReferenceExpression(getParam)),
							new CodeDirectionExpression(FieldDirection.In, new CodeArgumentReferenceExpression("MFDataType." + pdefAdmin.PropertyDef.DataType)),
							new CodePropertySetValueReferenceExpression()));
						break;
					case MFDataType.MFDatatypeBoolean:
						property.Type = new CodeTypeReference(typeof(bool));
						property.GetStatements.Add(new CodeMethodReturnStatement(
							new CodeFieldReferenceExpression(
								new CodeThisReferenceExpression(), string.Format("GetProperty({0}).Value.Value", getParam))));
						property.SetStatements.Add(new CodeMethodInvokeExpression(new CodeThisReferenceExpression(), "SetProperty",
							new CodeDirectionExpression(FieldDirection.In, new CodeArgumentReferenceExpression(getParam)),
							new CodeDirectionExpression(FieldDirection.In, new CodeArgumentReferenceExpression("MFDataType." + pdefAdmin.PropertyDef.DataType)),
							new CodePropertySetValueReferenceExpression()));
						break;
					case MFDataType.MFDatatypeInteger:
						property.Type = new CodeTypeReference(typeof(int));
						property.GetStatements.Add(new CodeMethodReturnStatement(
							new CodeFieldReferenceExpression(
								new CodeThisReferenceExpression(), string.Format("GetProperty({0}).Value.Value", getParam))));
						property.SetStatements.Add(new CodeMethodInvokeExpression(new CodeThisReferenceExpression(), "SetProperty",
							new CodeDirectionExpression(FieldDirection.In, new CodeArgumentReferenceExpression(getParam)),
							new CodeDirectionExpression(FieldDirection.In, new CodeArgumentReferenceExpression("MFDataType." + MFDataType.MFDatatypeInteger)),
							new CodePropertySetValueReferenceExpression()));
						break;
					case MFDataType.MFDatatypeFloating:
						property.Type = new CodeTypeReference(typeof(float));
						property.GetStatements.Add(new CodeMethodReturnStatement(
							new CodeFieldReferenceExpression(
								new CodeThisReferenceExpression(), string.Format("GetProperty({0}).Value.Value", getParam))));
						property.SetStatements.Add(new CodeMethodInvokeExpression(new CodeThisReferenceExpression(), "SetProperty",
							new CodeDirectionExpression(FieldDirection.In, new CodeArgumentReferenceExpression(getParam)),
							new CodeDirectionExpression(FieldDirection.In, new CodeArgumentReferenceExpression("MFDataType." + pdefAdmin.PropertyDef.DataType)),
							new CodePropertySetValueReferenceExpression()));
						break;
					case MFDataType.MFDatatypeLookup:
						ObjType objType = _vault.ObjectTypeOperations.GetObjectType(pdefAdmin.PropertyDef.ValueList);
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
								new CodeThisReferenceExpression(), string.Format("GetProperty({0}).Value.Value", getParam))));
						property.SetStatements.Add(new CodeMethodInvokeExpression(new CodeThisReferenceExpression(), "SetProperty",
							new CodeDirectionExpression(FieldDirection.In, new CodeArgumentReferenceExpression(getParam)),
							new CodeDirectionExpression(FieldDirection.In, new CodeArgumentReferenceExpression("MFDataType." + pdefAdmin.PropertyDef.DataType)),
							new CodePropertySetValueReferenceExpression()));
						break;
					case MFDataType.MFDatatypeMultiSelectLookup:
						ObjType objTypeMulti = _vault.ObjectTypeOperations.GetObjectType(pdefAdmin.PropertyDef.ValueList);
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
								new CodeThisReferenceExpression(), string.Format("GetProperty({0}).Value.Value", getParam))));
						property.SetStatements.Add(new CodeMethodInvokeExpression(new CodeThisReferenceExpression(), "SetProperty",
							new CodeDirectionExpression(FieldDirection.In, new CodeArgumentReferenceExpression(getParam)),
							new CodeDirectionExpression(FieldDirection.In, new CodeArgumentReferenceExpression("MFDataType." + pdefAdmin.PropertyDef.DataType)),
							new CodePropertySetValueReferenceExpression()));
						break;
					default:
						//throw new NotImplementedException(string.Format("Generation of datatype {0} not yet supported.", pdef.DataType));
						_command.WriteWarning(string.Format("Generation of datatype {0} not yet supported.", pdefAdmin.PropertyDef.DataType));
						break;
				}

				targetClass.Members.Add(property);
			}
		}
	}
}
