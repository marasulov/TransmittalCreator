using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace TransmittalCreator.Models;

public class BlockAttribute : INotifyPropertyChanged
{
	private string _attributeName;
	private string _attributeValue;

	public BlockAttribute(string attrName, string attrValue)
	{
		AttributeName = attrName;
		AttributeValue = attrValue;
	}

	/// <summary>
	///     Название атрибута
	/// </summary>
	public string AttributeName
	{
		get => _attributeName;
		set
		{
			_attributeName = value;
			OnPropertyChanged();
		}
	}

	/// <summary>
	///     Document Number
	/// </summary>
	public string AttributeValue
	{
		get => _attributeValue;
		set
		{
			_attributeValue = value;
			OnPropertyChanged();
		}
	}

	public event PropertyChangedEventHandler PropertyChanged;

	public string GetAttrName(BlockAttribute blockAttribute)
	{
		return blockAttribute.AttributeName;
	}

	public void OnPropertyChanged([CallerMemberName] string prop = "")
	{
		PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(prop));
	}
}