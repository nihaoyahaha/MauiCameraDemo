using Camera.MAUI;
using SkiaSharp;

namespace MauiApp1;

public partial class MainPage : ContentPage
{
	double panX, panY;
	public MainPage()
	{
		InitializeComponent();
	}
	//加载摄像头
	async private void cameraView_CamerasLoaded(object sender, EventArgs e)
	{
		if (cameraView.NumCamerasDetected > 0)
		{
			cameraView.Camera = cameraView.Cameras.First();
			txt_CameraCount.Text = cameraView.Cameras.Count.ToString();
			lb_CameraPosition.Text = "摄像头类型:" + cameraView.Camera.Position.ToString();
			var result = await cameraView.StartCameraAsync();
			string str = "";
			foreach (var item in cameraView.Camera.AvailableResolutions)
			{
				str += $"{item.Width}*{item.Height},";
			}
			lb_CameraSizes.Text += str;
		}
		else
		{
			txt_CameraCount.Text = "0";
		}

	}

	//拍照
	async private void OnCounterClicked(object sender, EventArgs e)
	{
		if (cameraView.NumCamerasDetected <= 0)
		{
			await DisplayAlert("提示", "无可用摄像头！", "确定");
			return;
		}
		var cameraViewStream = await cameraView.TakePhotoAsync();
		if (cameraViewStream != null)
		{
			SKBitmap bitmap = SKBitmap.Decode(cameraViewStream);
			using SKCanvas canvas = new(bitmap);
			int photoWidth = bitmap.Width;
			int photoHeight = bitmap.Height;

			float xOffset = (float)rectGrid.TranslationX / (float)cameraView.WidthRequest * photoWidth;
			float yOffset = (float)rectGrid.TranslationY / (float)cameraView.HeightRequest * photoHeight;

			IScreenshotResult screen = await rectGrid.CaptureAsync();
			Stream rectGridStream = await screen.OpenReadAsync();
			SKBitmap croppedBitmap = SKBitmap.Decode(rectGridStream);

			canvas.DrawBitmap(croppedBitmap, SKRect.Create(xOffset, yOffset, (float)rectGrid.Width, (float)rectGrid.Height));

			using MemoryStream memStream = new();
			using SKManagedWStream wstream = new(memStream);
			bitmap.Encode(wstream, SKEncodedImageFormat.Png, 100);
			byte[] data = memStream.ToArray();
			string path = "";
			string fileName = Path.GetRandomFileName();
#if IOS || MACCATALYST
			path = $"{FileSystem.AppDataDirectory}/{fileName}.png";
#elif WINDOWS
	        path = $"{FileSystem.AppDataDirectory}\\{fileName}.png";
#endif
			using FileStream writestream = new(path, FileMode.Create, FileAccess.Write);
			await writestream.WriteAsync(data, 0, data.Length);
			img2.Source = path;
			await DisplayAlert("提示", $"图片保存成功\n保存路径:{path}", "确定");
		}
	}

	//切换摄像头
	async void btn_ChangeCamera_Clicked(object sender, EventArgs e)
	{
		if (cameraView.NumCamerasDetected > 1)
		{
			if (cameraView.Camera.Position == CameraPosition.Front)
			{
				cameraView.Camera = cameraView.Cameras.FirstOrDefault(c => c.Position == CameraPosition.Back);
			}
			else
			{
				cameraView.Camera = cameraView.Cameras.FirstOrDefault(c => c.Position == CameraPosition.Front);
			}
			await cameraView.StartCameraAsync();
			string str = "";
			foreach (var item in cameraView.Camera.AvailableResolutions)
			{
				str += $"{item.Width}*{item.Height},";
			}
			lb_CameraSizes.Text += str;
			lb_CameraPosition.Text = "摄像头类型:" + cameraView.Camera.Position.ToString();
		}
	}

	//移动绿板
	private void PanGestureRecognizer_PanUpdated(object sender, PanUpdatedEventArgs e)
	{
		switch (e.StatusType)
		{
			case GestureStatus.Running:
				double boundsX = gridRoot.Width;
				double boundsY = gridRoot.Height;
				rectGrid.TranslationX = Math.Clamp(panX + e.TotalX, 0, gridRoot.Width - rectGrid.Width);
				rectGrid.TranslationY = Math.Clamp(panY + e.TotalY, 0, gridRoot.Height - rectGrid.Height);
				break;

			case GestureStatus.Completed:
				panX = rectGrid.TranslationX;
				panY = rectGrid.TranslationY;
				break;
		}
	}
}

