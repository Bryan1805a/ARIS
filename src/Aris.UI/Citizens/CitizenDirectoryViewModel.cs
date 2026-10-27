using Aris.Application.Citizens;
using Aris.Application.Citizens.Dtos;

namespace Aris.UI.Citizens;

public sealed class CitizenDetailViewModel
{
    private readonly ICitizenService _citizenService;
    public int CitizenId { get; }

    public CitizenDetailViewModel(ICitizenService citizenService, int citizenId)
    {
        _citizenService = citizenService;
        CitizenId = citizenId;
    }

    public async Task<CitizenDetailDto?> LoadCitizenDetailsAsync()
    {
        var result = await _citizenService.GetByIdAsync(CitizenId);
        if (result.IsFailure || result.Value is null)
        {
            MessageBox.Show("Không tìm thấy thông tin công dân.", "Lỗi", MessageBoxButtons.OK, MessageBoxIcon.Error);
            return null;
        }
        return result.Value;
    }

    public async Task<bool> UpdateCitizenAsync(string fullName, DateOnly dob, string gender)
    {
        var dto = new UpdateCitizenDto
        {
            FullName = fullName.Trim(),
            DateOfBirth = dob,
            Gender = gender
        };

        var result = await _citizenService.UpdateCitizenAsync(CitizenId, dto, currentUserId: 1);
        if (result.IsFailure)
        {
            MessageBox.Show(string.Join(Environment.NewLine, result.Errors), "Lỗi cập nhật", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            return false;
        }

        MessageBox.Show("Cập nhật thông tin công dân thành công.", "Thành công", MessageBoxButtons.OK, MessageBoxIcon.Information);
        return true;
    }

    public async Task<bool> DeactivateCitizenAsync()
    {
        var result = await _citizenService.DeactivateCitizenAsync(CitizenId, currentUserId: 1);
        if (result.IsFailure)
        {
            MessageBox.Show(string.Join(Environment.NewLine, result.Errors), "Lỗi vô hiệu hóa", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            return false;
        }

        MessageBox.Show("Đã vô hiệu hóa công dân thành công (Trạng thái chuyển sang INACTIVE).", "Thành công", MessageBoxButtons.OK, MessageBoxIcon.Information);
        return true;
    }
}