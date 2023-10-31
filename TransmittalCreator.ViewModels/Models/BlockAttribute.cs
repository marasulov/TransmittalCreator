namespace TransmittalCreator.ViewModels.Models
{
	public class BlockAttribute//: INotifyPropertyChanged
	{

		private string _attributeName;
		private string _attributeValue;

		/// <summary>
		/// Название атрибута
		/// </summary>
		public string AttributeName
		{
			get => _attributeName;
			set
			{
				_attributeName = value;
				//OnPropertyChanged("AttributeName");
			}
		}

		/// <summary>
		/// Document Number
		/// </summary>
		public string AttributeValue
		{
			get => _attributeValue;
			set
			{
				_attributeValue = value;
				//OnPropertyChanged("AttributeValue");
			}
		}

		public BlockAttribute(string attrName, string attrValue)
		{
			this.AttributeName = attrName;
			this.AttributeValue = attrValue;
		}
	}
}
