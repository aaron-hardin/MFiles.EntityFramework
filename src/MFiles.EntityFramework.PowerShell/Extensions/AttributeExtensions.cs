using System.CodeDom;
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
			codeAttrDecl.Arguments.Add(new CodeAttributeArgument("Name", new CodeArgumentReferenceExpression(objClassAdmin.Name.Escape())));
			targetClass.CustomAttributes.Add(codeAttrDecl);
		}

		public static void AddAsAttribute(this ObjTypeAdmin ota, CodeTypeDeclaration targetClass)
		{
			CodeAttributeDeclaration codeAttrDecl = new CodeAttributeDeclaration("MetaStructureObjectType");
			codeAttrDecl.Arguments.Add(new CodeAttributeArgument("Name", new CodeArgumentReferenceExpression(ota.ObjectType.NameSingular.Escape())));
			targetClass.CustomAttributes.Add(codeAttrDecl);
		}

		public static void AddAsAttribute(this PropertyDefAdmin pda, CodeMemberProperty targetProperty, AssociatedPropertyDef apd = null)
		{
			CodeAttributeDeclaration codeAttrDecl = new CodeAttributeDeclaration("MetaStructureProperty");
			codeAttrDecl.Arguments.Add(new CodeAttributeArgument("Name", new CodeArgumentReferenceExpression(pda.PropertyDef.Name.Escape())));
			if (apd != null)
			{
				codeAttrDecl.Arguments.Add(new CodeAttributeArgument("Required",
					new CodeArgumentReferenceExpression(apd.Required.ToString().ToLower())));
			}
			targetProperty.CustomAttributes.Add(codeAttrDecl);
		}

		public static xObjectClassAdmin AttributeToComModel(this MetaStructureClassAttribute attribute)
		{
			xObjectClassAdmin objClass = new xObjectClassAdmin
			{
				Name = attribute.Name
			};

			return objClass;
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
