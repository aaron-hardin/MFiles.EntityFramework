using System;
using System.CodeDom;
using System.Collections.Generic;
using System.Reflection;
using MFiles.EntityFramework.Design;
using MFiles.VaultJsonTools.ComModels;
using MFilesAPI;

namespace MFiles.EntityFramework.PowerShell.Extensions
{
	public static class AttributeExtensions
	{
		public static void AddAsAttribute(this ObjectClassAdmin objClassAdmin, ObjectClass objClass, CodeTypeDeclaration targetClass)
		{
			CodeAttributeDeclaration codeAttrDecl = new CodeAttributeDeclaration("MetaStructureClass");
			codeAttrDecl.Arguments.Add(new CodeAttributeArgument("Name", new CodePrimitiveExpression(objClassAdmin.Name)));
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
				Name = attribute.Name
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
