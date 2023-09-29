namespace TransmittalCreator.ViewModels
{
	public interface IRenumberPagesViewModel
	{
		int RenumberStartValue { get; set; }
		bool RenumberLayouts { get; set; }
		bool InsertTotalPages { get; set; }
	}
}
