#import <UIKit/UIKit.h>
extern "C" UIViewController* UnityGetGLViewController();
extern "C" void UnitySendMessage(const char*, const char*, const char*);
@interface MeteorContentPicker : NSObject<UIDocumentPickerDelegate>
@property(nonatomic,strong) NSURL *accessURL;
@end
@implementation MeteorContentPicker
- (void)documentPicker:(UIDocumentPickerViewController*)controller didPickDocumentsAtURLs:(NSArray<NSURL*>*)urls {
 if(urls.count==0)return;
 self.accessURL=urls.firstObject;
 [self.accessURL startAccessingSecurityScopedResource];
 UnitySendMessage("MeteorLauncher","FolderSelected",self.accessURL.path.UTF8String);
}
- (void)documentPickerWasCancelled:(UIDocumentPickerViewController*)controller {UnitySendMessage("MeteorLauncher","FolderSelectionCancelled","");}
@end
static MeteorContentPicker* picker;
extern "C" void MeteorEndContentFolderAccess(){if(picker.accessURL){[picker.accessURL stopAccessingSecurityScopedResource];picker.accessURL=nil;}}
extern "C" void MeteorPickContentFolder(){dispatch_async(dispatch_get_main_queue(), ^{
 if(!picker)picker=[MeteorContentPicker new];
 UIDocumentPickerViewController* view=[[UIDocumentPickerViewController alloc] initWithDocumentTypes:@[@"public.folder"] inMode:UIDocumentPickerModeOpen];
 view.delegate=picker;view.allowsMultipleSelection=NO;
 [UnityGetGLViewController() presentViewController:view animated:YES completion:nil];
});}

extern "C" void MeteorExcludeContentBackup(const char* path){NSURL* url=[NSURL fileURLWithPath:[NSString stringWithUTF8String:path]];NSError* error=nil;[url setResourceValue:@YES forKey:NSURLIsExcludedFromBackupKey error:&error];if(error)NSLog(@"Meteor resource backup exclusion failed: %@",error.localizedDescription);}

extern "C" int MeteorHasLaunchArgument(const char* value){return [[[NSProcessInfo processInfo] arguments] containsObject:[NSString stringWithUTF8String:value]]?1:0;}

// Unity 2020 IL2CPP DriveInfo.GetDrives traps on current iOS. Query only our container.
extern "C" long long MeteorAvailableContentBytes(const char* path){@autoreleasepool{NSError* error=nil;NSDictionary* info=[[NSFileManager defaultManager] attributesOfFileSystemForPath:[NSString stringWithUTF8String:path] error:&error];return info&&!error?[info[NSFileSystemFreeSize] longLongValue]:-1;}}
