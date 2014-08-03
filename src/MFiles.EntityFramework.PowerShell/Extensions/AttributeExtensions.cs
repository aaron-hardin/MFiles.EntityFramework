using System;
using System.CodeDom;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using MFiles.EntityFramework.Design;
using MFiles.VaultJsonTools.ComModels;
using MFilesAPI;

namespace MFiles.EntityFramework.PowerShell.Extensions
{
	public static class AttributeExtensions
	{
		public static void AddAsAttribute(this CodeTypeDeclaration targetClass, ObjectClassAdmin objClassAdmin, ObjectClass objClass)
		{
			CodeAttributeDeclaration codeAttrDecl = new CodeAttributeDeclaration("MetaStructureClass");
			codeAttrDecl.Arguments.Add(new CodeAttributeArgument("Name", new CodePrimitiveExpression(objClassAdmin.Name)));
			codeAttrDecl.Arguments.Add(new CodeAttributeArgument("ForceWorkflow", new CodePrimitiveExpression(objClassAdmin.ForceWorkflow)));
			codeAttrDecl.Arguments.Add(new CodeAttributeArgument("ID", new CodePrimitiveExpression(objClassAdmin.ID)));
			codeAttrDecl.Arguments.Add(new CodeAttributeArgument("NamePropertyDef", new CodePrimitiveExpression(objClassAdmin.NamePropertyDef)));
			codeAttrDecl.Arguments.Add(new CodeAttributeArgument("ObjectType", new CodePrimitiveExpression(objClassAdmin.ObjectType)));
			codeAttrDecl.Arguments.Add(new CodeAttributeArgument("Predefined", new CodePrimitiveExpression(objClassAdmin.Predefined)));
			codeAttrDecl.Arguments.Add(new CodeAttributeArgument("SemanticAliases", new CodePrimitiveExpression(objClassAdmin.SemanticAliases.Value.Split(';').ToArray())));
			codeAttrDecl.Arguments.Add(new CodeAttributeArgument("Workflow", new CodePrimitiveExpression(objClassAdmin.Workflow)));
			targetClass.CustomAttributes.Add(codeAttrDecl);
		}

		public static void AddAsAttribute(this ObjTypeAdmin ota, CodeTypeDeclaration targetClass)
		{
			CodeAttributeDeclaration codeAttrDecl = new CodeAttributeDeclaration("MetaStructureObjectType");
			codeAttrDecl.Arguments.Add(new CodeAttributeArgument("NameSingular", new CodePrimitiveExpression(ota.ObjectType.NameSingular)));
			codeAttrDecl.Arguments.Add(new CodeAttributeArgument("NamePlural", new CodePrimitiveExpression(ota.ObjectType.NamePlural)));
			targetClass.CustomAttributes.Add(codeAttrDecl);
		}

		public static void AddAsAttribute(this AssociatedPropertyDef apd, CodeMemberProperty targetProperty)
		{
			CodeAttributeDeclaration codeAttrDecl = new CodeAttributeDeclaration("MetaStructureAssociatedProperty");
			codeAttrDecl.Arguments.Add(new CodeAttributeArgument("PropertyDef", new CodePrimitiveExpression(apd.PropertyDef)));
			codeAttrDecl.Arguments.Add(new CodeAttributeArgument("Required", new CodePrimitiveExpression(apd.Required)));
			
			targetProperty.CustomAttributes.Add(codeAttrDecl);
		}

		public static xObjectClassAdmin AttributeToComModel(this Type type)
		{
			MetaStructureClassAttribute attr = type.GetCustomAttribute<MetaStructureClassAttribute>();
			xObjectClassAdmin metaClass = attr.AttributeToComModel();

			List<xAssociatedPropertyDef> associatedProperties = new List<xAssociatedPropertyDef>();

			PropertyInfo[] properties = type.GetProperties();
			foreach (PropertyInfo property in properties)
			{
				MetaStructureAssociatedPropertyAttribute propAttr =
					property.GetCustomAttribute<MetaStructureAssociatedPropertyAttribute>();
				associatedProperties.Add(propAttr.AttributeToComModel());
			}

			metaClass.AssociatedPropertyDefs = associatedProperties.ToArray();

			return metaClass;
		}

		public static xObjectClassAdmin AttributeToComModel(this MetaStructureClassAttribute attribute)
		{
			xObjectClassAdmin objClass = new xObjectClassAdmin
			{
				Name = attribute.Name,
				ForceWorkflow = attribute.ForceWorkflow,
				ID = attribute.ID,
				NamePropertyDef = attribute.NamePropertyDef,
				ObjectType = attribute.ObjectType,
				Predefined = attribute.Predefined,
				SemanticAliases = attribute.SemanticAliases,
				Workflow = attribute.Workflow
			};

			return objClass;
		}

		public static xAssociatedPropertyDef AttributeToComModel(this MetaStructureAssociatedPropertyAttribute attribute)
		{
			xAssociatedPropertyDef prop = new xAssociatedPropertyDef
			{
				PropertyDef = attribute.PropertyDef,
				Required = attribute.Required
			};
			return prop;
		}

		public static xObjTypeAdmin AttributeToComModel(this MetaStructureObjectTypeAttribute attribute)
		{
			xObjTypeAdmin objType = new xObjTypeAdmin
			{
				
			};

			objType.ObjectType = new xObjType
			{
				NamePlural = attribute.NamePlural,
				NameSingular = attribute.NameSingular
			};

			return objType;
		}

		public static xPropertyDefAdmin AttributeToComModel(this MetaStructurePropertyAttribute attribute)
		{
			xPropertyDefAdmin prop = new xPropertyDefAdmin
			{
				
			};

			prop.PropertyDef = new xPropertyDef
			{
				Name = attribute.Name
			};

			return prop;
		}
	}
}
