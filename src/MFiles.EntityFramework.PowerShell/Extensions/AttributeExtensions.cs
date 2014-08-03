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
					new CodeArgumentReferenceExpression(apd.Required.ToString())));
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
	}
}
