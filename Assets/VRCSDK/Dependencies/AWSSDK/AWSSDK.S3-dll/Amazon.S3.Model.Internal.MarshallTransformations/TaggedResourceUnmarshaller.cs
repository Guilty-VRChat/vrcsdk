using Amazon.Runtime.Internal.Transform;

namespace Amazon.S3.Model.Internal.MarshallTransformations
{
	internal class TaggedResourceUnmarshaller : IUnmarshaller<TaggedResource, XmlUnmarshallerContext>
	{
		private static TaggedResourceUnmarshaller _instance;

		public static TaggedResourceUnmarshaller Instance
		{
			get
			{
				if (_instance == null)
				{
					_instance = new TaggedResourceUnmarshaller();
				}
				return _instance;
			}
		}

		public TaggedResource Unmarshall(XmlUnmarshallerContext context)
		{
			TaggedResource taggedResource = new TaggedResource();
			int currentDepth = context.get_CurrentDepth();
			int num = currentDepth + 1;
			if (context.get_IsStartOfDocument())
			{
				num += 2;
			}
			while (context.Read())
			{
				if (context.get_IsStartElement() || context.get_IsAttribute())
				{
					if (context.TestExpression("Key", num))
					{
						taggedResource.Key = StringUnmarshaller.GetInstance().Unmarshall(context);
					}
					else if (context.TestExpression("VersionId", num))
					{
						taggedResource.VersionId = StringUnmarshaller.GetInstance().Unmarshall(context);
					}
					else if (context.TestExpression("Tags", num))
					{
						taggedResource.Tags.Add(TagUnmarshaller.Instance.Unmarshall(context));
					}
				}
				else if (context.get_IsEndElement() && context.get_CurrentDepth() < currentDepth)
				{
					return taggedResource;
				}
			}
			return taggedResource;
		}
	}
}
