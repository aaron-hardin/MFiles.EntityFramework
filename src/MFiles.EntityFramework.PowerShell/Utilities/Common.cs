using System.CodeDom;
using MFilesAPI;

namespace MFiles.EntityFramework.PowerShell.Utilities
{
	internal class Common
	{
		/// <summary> 
		/// Add constructors to the class. 
		/// </summary> 
		internal static void AddConstructors(CodeTypeDeclaration targetClass)
		{
			// Declare the constructor
			CodeConstructor constructor = new CodeConstructor
			{
				Attributes = MemberAttributes.Public | MemberAttributes.Final
			};

			// Add parameters.
			constructor.Parameters.Add(new CodeParameterDeclarationExpression(
				typeof(Vault), "vault"));
			constructor.Parameters.Add(new CodeParameterDeclarationExpression(
				typeof(ObjectVersion), "versionInfo"));
			constructor.Parameters.Add(new CodeParameterDeclarationExpression(
				typeof(bool), "checkOut = false"));

			constructor.BaseConstructorArgs.Add(new CodeArgumentReferenceExpression("vault"));
			constructor.BaseConstructorArgs.Add(new CodeArgumentReferenceExpression("versionInfo"));
			constructor.BaseConstructorArgs.Add(new CodeArgumentReferenceExpression("checkOut"));

			targetClass.Members.Add(constructor);

			// Declare the constructor
			CodeConstructor constructor2 = new CodeConstructor
			{
				Attributes = MemberAttributes.Public | MemberAttributes.Final
			};

			// Add parameters.
			constructor2.Parameters.Add(new CodeParameterDeclarationExpression(
				typeof(ObjectVersionAndProperties), "ovap"));
			constructor2.Parameters.Add(new CodeParameterDeclarationExpression(
				typeof(bool), "checkOut = false"));

			constructor2.BaseConstructorArgs.Add(new CodeArgumentReferenceExpression("ovap"));
			constructor2.BaseConstructorArgs.Add(new CodeArgumentReferenceExpression("checkOut"));

			targetClass.Members.Add(constructor2);

			// Declare the constructor
			CodeConstructor constructor3 = new CodeConstructor
			{
				Attributes = MemberAttributes.Public | MemberAttributes.Final
			};

			// Add parameters.
			constructor3.Parameters.Add(new CodeParameterDeclarationExpression(
				typeof(Vault), "vault"));
			constructor3.Parameters.Add(new CodeParameterDeclarationExpression(
				typeof(ObjVer), "objVer"));
			constructor3.Parameters.Add(new CodeParameterDeclarationExpression(
				typeof(bool), "checkOut = false"));

			constructor3.BaseConstructorArgs.Add(new CodeArgumentReferenceExpression("vault"));
			constructor3.BaseConstructorArgs.Add(new CodeArgumentReferenceExpression("objVer"));
			constructor3.BaseConstructorArgs.Add(new CodeArgumentReferenceExpression("checkOut"));

			targetClass.Members.Add(constructor3);

			// Declare the constructor
			CodeConstructor constructor4 = new CodeConstructor
			{
				Attributes = MemberAttributes.Public | MemberAttributes.Final
			};

			// Add parameters.
			constructor4.Parameters.Add(new CodeParameterDeclarationExpression(
				typeof(Vault), "vault"));
			constructor4.Parameters.Add(new CodeParameterDeclarationExpression(
				typeof(int), "objType"));
			constructor4.Parameters.Add(new CodeParameterDeclarationExpression(
				typeof(int), "id"));
			constructor4.Parameters.Add(new CodeParameterDeclarationExpression(
				typeof(int), "version"));
			constructor4.Parameters.Add(new CodeParameterDeclarationExpression(
				typeof(bool), "checkOut = false"));

			constructor4.BaseConstructorArgs.Add(new CodeArgumentReferenceExpression("vault"));
			constructor4.BaseConstructorArgs.Add(new CodeArgumentReferenceExpression("objType"));
			constructor4.BaseConstructorArgs.Add(new CodeArgumentReferenceExpression("id"));
			constructor4.BaseConstructorArgs.Add(new CodeArgumentReferenceExpression("version"));
			constructor4.BaseConstructorArgs.Add(new CodeArgumentReferenceExpression("checkOut"));

			targetClass.Members.Add(constructor4);
		}
	}
}
