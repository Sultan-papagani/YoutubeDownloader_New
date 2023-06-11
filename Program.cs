using System;
using System.Net;
using System.Drawing;
using System.Drawing.Imaging;
using AngleSharp.Text;
using Pastel;
using YoutubeExplode;
using YoutubeExplode.Videos;
using YoutubeExplode.Videos.Streams;
using static System.Net.Mime.MediaTypeNames;
using System.Text.RegularExpressions;

/*
 0 Video indir 720p ve altı
 1 Video indir 1080p + ffmpeg
 2 Video Thumbnail indir
 */


class YoutubeDownloader
{
    static string CACHEPATH = AppDomain.CurrentDomain.BaseDirectory + @"Cache\";
    static string VIDEOPATH = AppDomain.CurrentDomain.BaseDirectory + @"Output\";
    static string IMAGEPATH = AppDomain.CurrentDomain.BaseDirectory + @"Images\";

    static string YELLOW = "FFD000";
    static string GREEN = "#63C132";
    static string CYAN = "#048A81";

    static string CYAN1 = "#0086ff";
    static string PINK = "#ff0061";

    static string image_header = "https://i1.ytimg.com/vi/";
    static string[] image_ends = { "/default.jpg", "/mqdefault.jpg", "/hqdefault.jpg", "/sddefault.jpg", "/maxresdefault.jpg" };

    static string seperator = "▬▬▬▬▬▬▬▬▬▬▬▬▬▬▬▬▬▬▬▬▬▬".Pastel(CYAN1);

    static string Header_Home = "[HOME]→ ".Pastel(GREEN);
    static string Error = "[ERROR]→ ".Pastel(ConsoleColor.Red);
    static string OK = "[OK]→ ".Pastel(PINK);

    static string App_Title = "Video Downloader® - Version 3.1";

    static string Welcome_Text = "Welcome to Video Downloader Home Page\n";
    static string HomePage_Selections = "0 → ".Pastel(YELLOW) + "Download video - Max 720p\n" + "1 → ".Pastel(YELLOW) + "Download video with ffmpeg\n" + "2 → ".Pastel(YELLOW) + "Download Thumbnail" + "";
    static string LinkSelection = "Video Link: ";

    static string audio_info_title = string.Format("{0} {1, -15} {2, -15} {3, -15} {4, -15}", "* -", "AudioCodec".Pastel(PINK), "Bitrate".Pastel(CYAN), "Container".Pastel(YELLOW), "Size".Pastel(GREEN));
    static string video_info_title = string.Format("{0} {1, -15} {2, -15} {3, -15} {4, -15} {5, 15} {6, -15}", "* -", "VideoCodec'i".Pastel(PINK), "VideoKalitesi".Pastel(CYAN), "VideoCozunurlugu".Pastel(YELLOW), "Boyutu".Pastel(GREEN), "Bitrate".Pastel(YELLOW), "Conatiner".Pastel(CYAN1));

    public static char qte = (char)34;
    public static string[] blacklist = { @"\", "/", "[", "]", "{", "}", "|", ">", "<", ":", "*", "=", "-", "-", "?", "*", "(", ")", "-", "-", qte.ToString() };

    static Video downladed_video;
    static MuxedStreamInfo[] muxed_list;

    static AudioOnlyStreamInfo[] only_auido_list;
    static VideoOnlyStreamInfo[] only_video_list;

    static string ffmpeg_audio_path;
    static string ffmpeg_video_path;
    static string ffmpeg_video_name;

    static void Main(string[] args)
    {
        Console.Title = App_Title;
        HomePage();
    }

    static void HomePage()
    {
        Console.Clear();
        Console.WriteLine(Header_Home+Welcome_Text);
        Console.WriteLine(HomePage_Selections);
        string selection = Console.ReadLine();
        Console.WriteLine(Header_Home + LinkSelection);
        string video_link = Console.ReadLine();
        if (int.TryParse(selection, out int int_selection))
        {
            switch (int_selection)
            {
                case 0:
                    DownloadVideoMux(video_link);
                    break;

                case 1:
                    DownloadVideoSeperately(video_link);
                    break;

                case 2:
                    DownloadThumbnail(video_link);
                    break;

                default:
                    HomePage();
                    break;
            }
        }
        else
        {
            HomePage();
        }
        
    }

    async static Task PrintVideoInfo(YoutubeClient client, string link, bool seperate = false)
    {
        string options;

        downladed_video = await client.Videos.GetAsync(link);
        var streamManifest = await client.Videos.Streams.GetManifestAsync(link);

        // headerler
        Console.WriteLine(seperator);
        Console.WriteLine("\n");
        Console.WriteLine("Video Name: ".Pastel(PINK));
        Console.WriteLine(downladed_video.Title);
        Console.WriteLine("Video Duration: ".Pastel(CYAN1) + downladed_video.Duration);
        Console.WriteLine("Video Author: ".Pastel(YELLOW) + downladed_video.Author);
        Console.WriteLine("Upload Date: ".Pastel(GREEN) + downladed_video.UploadDate+ "\n\n");

        // muxed stream için headeri yazdır
        Console.WriteLine(seperator);

        int selection_index = 0;

        // muxed stream seçeneklerini sırala
        if (!seperate)
        {
            Console.WriteLine(video_info_title);

          
            var muxed_streams = streamManifest.GetMuxedStreams();
            muxed_list = muxed_streams.ToArray();

            foreach (var vid in muxed_streams)
            {
                options = string.Format("{0, -15} {1, -15} {2, -15} {3, -15} {4, -15} {5, -15}", vid.VideoCodec, vid.VideoQuality, vid.VideoResolution, vid.Size, vid.Bitrate, vid.Container);
                Console.WriteLine($"{selection_index} - {options}");
                selection_index++;
            }
        }
        else // seperated videos
        {
            IVideoStreamInfo best_quality_video = streamManifest.GetVideoOnlyStreams().GetWithHighestVideoQuality();
            IStreamInfo best_bitrate_video = streamManifest.GetVideoOnlyStreams().GetWithHighestBitrate();
            IStreamInfo best_sound_bitrate = streamManifest.GetAudioOnlyStreams().GetWithHighestBitrate();

            Console.WriteLine("(avc1 better)");
            Console.WriteLine($"Best Quality Video -> codec:{best_quality_video.VideoCodec}, size:{best_quality_video.Size}, bitrate:{best_quality_video.Bitrate}, resolution:{best_quality_video.VideoResolution}, container:{best_quality_video.Container}");
            Console.WriteLine($"Best Bitrate Video -> size:{best_bitrate_video.Size}, bitrate:{best_quality_video.Bitrate}, container:{best_quality_video.Container}");
            Console.WriteLine();
            Console.WriteLine("(opus better)");
            Console.WriteLine($"Best Bitrate Audio -> size:{best_sound_bitrate.Size}, bitrate:{best_sound_bitrate.Bitrate}, container:{best_sound_bitrate.Container}");
            Console.WriteLine(seperator);
            var auido_only_streams = streamManifest.GetAudioOnlyStreams();
            var video_only_streams = streamManifest.GetVideoOnlyStreams();

            only_auido_list = auido_only_streams.ToArray();
            only_video_list = video_only_streams.ToArray();

            // videolar
            Console.WriteLine("\n"+video_info_title);
            foreach (var _video in video_only_streams)
            {
                //_video.Bitrate
                //_video.Container
                options = string.Format("{0, -15} {1, -15} {2, -15} {3, -15} {4, -15} {5, -15}", _video.VideoCodec, _video.VideoQuality, _video.VideoResolution, _video.Size, _video.Bitrate, _video.Container);
                Console.WriteLine($"{selection_index} - {options}");
                selection_index++;
            }

            
            selection_index = 0;

            // sesler
            Console.WriteLine("\n"+audio_info_title);
            foreach (var audio in auido_only_streams)
            {
                options = string.Format("{0, -15} {1, -15} {2, -15} {3, -15}", audio.AudioCodec, audio.Bitrate, audio.Container, audio.Size);
                Console.WriteLine($"{selection_index} - {options}");
                selection_index++;
            }


        }


        Console.WriteLine("\n"+seperator);

    }

    static async Task DownloadMuxedStream(YoutubeClient youtube,int index)
    {
        MuxedStreamInfo video = muxed_list[index];

        string pathToVideo = VIDEOPATH + filter_text(downladed_video.Title) + "." + video.Container;
        if (File.Exists(pathToVideo))
        {
            ErrorReturn("video already exist");
        }
        else
        {
            await youtube.Videos.Streams.DownloadAsync(video, pathToVideo);
        }
    }


    static void DownloadVideoSeperately(string link)
    {
        YoutubeClient youtube = new YoutubeClient();
        var pr1 = PrintVideoInfo(youtube, link, true);
        pr1.Wait();
        Console.WriteLine($"\n{Header_Home} Video Secimi Yapiniz 0 - {only_video_list.Count() - 1}");
        string video_response = Console.ReadLine();
        Console.WriteLine($"\n{Header_Home} Ses Secimi Yapiniz 0 - {only_auido_list.Count() - 1}");
        string auido_response = Console.ReadLine();

        int auido_index = 0, video_index = 0;
        if (int.TryParse(video_response, out int vid_response))
        {
            video_index = vid_response;
        }
        else { ErrorReturn("cannot parse to int"); }

        if (int.TryParse(auido_response, out int a_response))
        {
            auido_index = a_response;
        }
        else { ErrorReturn("cannot parse to int"); }
        if (auido_index < 0 || auido_index > only_auido_list.Count() - 1) { ErrorReturn("index out of range for auido"); }
        if (video_index < 0 || video_index > only_video_list.Count() - 1) { ErrorReturn("index out of range for video"); }

        var pr2 = DownloadVideoAndAuido(youtube, auido_index, video_index);
        pr2.Wait();
        Console.WriteLine(seperator+"\n");
        Console.WriteLine($"{OK} Dosyalar birlestiriliyor");
        ffmpeg_birlestir();
        Console.WriteLine($"{OK} videolar birlestirildi. cache siliniyor");
        DeleteCache();
        Console.WriteLine($"{OK} cache silindi");
        SucsessReturn("Video downloaded sucsessfuly");
    }

    static void ffmpeg_birlestir()
    {
        string finalLocation = VIDEOPATH + "FINAL " + ffmpeg_video_name + ".mp4"; 
        if (File.Exists(finalLocation))
        {
            ErrorReturn("video already exist");
        }
        var prcs = System.Diagnostics.Process.Start("ffmpeg.exe", $" -i {qte}{ffmpeg_video_path}{qte} -i {qte}{ffmpeg_audio_path}{qte} -c:v copy -c:a aac {qte}{finalLocation}{qte}");
        prcs.WaitForExit();
    }

    public static async Task DownloadVideoAndAuido(YoutubeClient youtube,int auido_index, int video_index)
    {
        AudioOnlyStreamInfo ses = only_auido_list[auido_index];
        VideoOnlyStreamInfo video = only_video_list[video_index];

        string video_name = filter_text(downladed_video.Title);
        string pathToAudio = CACHEPATH + "AUDIO" + video_name + "." + ses.Container;
        string pathToVideo = CACHEPATH + "VIDEO" + video_name + "." + video.Container;

        ffmpeg_audio_path = pathToAudio;
        ffmpeg_video_path = pathToVideo;
        ffmpeg_video_name = video_name;

        if (File.Exists(pathToAudio) || File.Exists(pathToVideo))
        {
            ErrorReturn("video already exist");
        }
        else
        {
            Console.WriteLine($"{OK} Ses dosyasi indiriliyor");
            await youtube.Videos.Streams.DownloadAsync(ses, pathToAudio);
            Console.WriteLine($"{OK} Video dosyasi indiriliyor");
            await youtube.Videos.Streams.DownloadAsync(video, pathToVideo);
        }
    }


    static void DownloadVideoMux(string link)
    {
        YoutubeClient youtube = new YoutubeClient();
        var pr1 = PrintVideoInfo(youtube,link);
        pr1.Wait();
        Console.WriteLine($"\n{Header_Home} Secim Yapiniz 0 - {muxed_list.Count() - 1}");
        string response = Console.ReadLine();
        if (int.TryParse(response, out int result))
        {
            if (result > muxed_list.Count() - 1 && result < 0)
            {
                ErrorReturn("index out of range");
            }
            else
            {
                var pr2 = DownloadMuxedStream(youtube, result);
                pr2.Wait();
                SucsessReturn("video downloaded sucsessfuly");
            }
        }
        else
        {
            ErrorReturn("cannot parse to int");
        }
    }

    static string filter_text(string text)
    {
        string x = text;
        x = x.Trim();
        x = x.StripLineBreaks();
        foreach(string filter in blacklist)
        {
            x = text.Replace(filter, "");
        }
        string s1 = Regex.Replace(x, "[^A-Za-z0-9 -]", "");
        return s1;
    }

    static void SucsessReturn(string why)
    {
        Console.WriteLine("\n+" + $"{OK} {why}");
        Console.ReadLine();
        HomePage();
    }

    static void ErrorReturn(string why)
    {
        Console.WriteLine("\n+" + $"{Error} {why}");
        Console.ReadLine();
        HomePage();
    }

    async static Task<string[]> GetVideoID(string link)
    {
        YoutubeClient youtube = new YoutubeClient();
        var video = await youtube.Videos.GetAsync(link);
        string titlefixed = video.Title;
        foreach (string b in blacklist)
        {
            titlefixed = titlefixed.Replace(b, " ");
        }
        string[] x = { video.Id, titlefixed };
        return x;
    }

    static void DownloadThumbnail(string link)
    {
        var x = GetVideoID(link);
        x.Wait();
        Console.WriteLine(seperator);
        Console.WriteLine($"Video id degeri = {x.Result[0]}\nVideo Adi = {x.Result[1]}\nFotograf boyutu secin:\n");
        Console.WriteLine(seperator);
        Console.WriteLine("0: default\n1:medium\n2:high\n3:standard\n"+"4:max res".Pastel(YELLOW));
        string select = Console.ReadLine();
        if (int.TryParse(select, out int result))
        {
            if (result <= 4)
            {
                using (WebClient webClient = new WebClient())
                {
                    byte[] data = webClient.DownloadData(image_header + x.Result[0] + image_ends[result]);
                    using (MemoryStream mem = new MemoryStream(data))
                    {
                        using (var yourImage = System.Drawing.Image.FromStream(mem))
                        {
                            if (File.Exists(IMAGEPATH + x.Result[1] + ".png"))
                            {
                                ErrorReturn("image already exist");
                            }
                            yourImage.Save(IMAGEPATH+ x.Result[1] + ".png", ImageFormat.Png);
                            SucsessReturn("fotograf indirildi");
                        }
                    }

                }
            }
            else
            {
                ErrorReturn("index out of range");
            }
        }
        else
        {
            ErrorReturn("cannot parse for int");
        }
    }

    public static void DeleteCache()
    {
        DirectoryInfo di = new DirectoryInfo(CACHEPATH);
        foreach (FileInfo file in di.GetFiles())
        {
            file.Delete();
        }
    }
}

