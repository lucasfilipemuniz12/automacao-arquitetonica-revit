using System.IO;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using Autodesk.Revit.DB;

namespace Vebram.Revit;

internal static class SheetPreviewImages
{
 internal static Dictionary<long,ImageSource> Read(Document doc,SheetPlan plan)
 {
  var images=new Dictionary<long,ImageSource>();
  var folder=Path.Combine(Path.GetTempPath(),"VebramPreview-"+Guid.NewGuid().ToString("N"));
  Directory.CreateDirectory(folder);
  try
  {
   foreach(var drawing in plan.Pages.SelectMany(p=>p.Drawings).Where(d=>d.Scale>0).DistinctBy(d=>d.ViewId))
   {
    try
    {
     string name="vista-"+drawing.ViewId;
     using var options=new ImageExportOptions
     {
      FilePath=Path.Combine(folder,name),ExportRange=ExportRange.SetOfViews,PixelSize=320,ZoomType=ZoomFitType.FitToPage,
      HLRandWFViewsFileType=ImageFileType.PNG,ShadowViewsFileType=ImageFileType.PNG
     };
     options.SetViewsAndSheets([new ElementId(drawing.ViewId)]);
     doc.ExportImage(options);
     var file=Directory.EnumerateFiles(folder,name+"*.png").FirstOrDefault();
     if(file==null)continue;
     using var stream=File.OpenRead(file);
     var image=new BitmapImage();
     image.BeginInit();image.CacheOption=BitmapCacheOption.OnLoad;image.StreamSource=stream;image.EndInit();image.Freeze();
     images[drawing.ViewId]=image;
    }
    catch(Exception ex) when(ex is Autodesk.Revit.Exceptions.ApplicationException or IOException or NotSupportedException)
    {
     // A view that cannot be exported still shows its measured position, outline and title.
    }
   }
  }
  finally
  {
   foreach(var file in Directory.EnumerateFiles(folder))File.Delete(file);
   Directory.Delete(folder);
  }
  return images;
 }
}
