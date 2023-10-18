﻿using Azure.Core;
using FFmpegWebAPI.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.OpenApi.Expressions;
using System;
using System.Diagnostics;
using System.Xml.Xsl;

internal class Program
{
    private static void Main(string[] args)
    {
        var builder = WebApplication.CreateBuilder(args);

        // Add services to the container.

        builder.Services.AddControllers();
        // Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle
        builder.Services.AddEndpointsApiExplorer();
        builder.Services.AddSwaggerGen();
        builder.Services.AddDbContext<IOTContext>(options =>
                  options.UseSqlServer(builder.Configuration.GetConnectionString("IOTConnection")));

        var app = builder.Build();
      
        // Configure the HTTP request pipeline.
        if (app.Environment.IsDevelopment())
        {
            app.UseSwagger();
            app.UseSwaggerUI();
        }
        
        app.UseStaticFiles();
        app.UseHttpsRedirection();

        app.UseAuthorization();

        app.MapControllers();

        var rootPath = app.Environment.ContentRootPath;
        //GetVideo();
        var contentRoot = rootPath + "\\Videos\\Test.mp4";
        TestCMD(contentRoot);
        app.Run();

       

    }
    public static string cmd = "cmd.exe";

    public static void TestCMD(string contentRoot)
    {
        string cmdLine = $@"-t 5 -rtsp_transport tcp -i rtsp://admin:Hd123456@10.68.81.193:554/ -vf scale=1280:720 -r 24 -crf 23 -maxrate 1M -bufsize 2M {contentRoot} -y -loglevel verbose";
        //string cmdLine = @"/C ""C:\Data\Files\text.txt""";
        Process process = new();
        var processStartInfo = new ProcessStartInfo(@"C:\Data\FFmpeg\bin\ffmpeg.exe");
        processStartInfo.WindowStyle = System.Diagnostics.ProcessWindowStyle.Hidden;
        processStartInfo.Arguments = cmdLine;
        process.StartInfo = processStartInfo;
        process.Start();
    }
    public static string GetVideo()
    {
        string cmdLine = "ffmpeg -t 5 -rtsp_transport tcp -i rtsp://admin:Hd123456@10.68.81.193:554/ -vf scale=1280:720 -r 24 -crf 23 -maxrate 1M -bufsize 2M C:/DataVideo/MyVideoFFmpeg.mp4 -y -loglevel verbose";
        Process process = new();
        ProcessStartInfo processStartInfo = new();
        processStartInfo.FileName = cmd;
        //Chạy ngầm cmd
        processStartInfo.CreateNoWindow = true;
        processStartInfo.UseShellExecute = false;
        //Cho phép nhận kết quả từ cmd
        processStartInfo.RedirectStandardInput = true;
        processStartInfo.RedirectStandardOutput = true;
      
        process.StartInfo = processStartInfo;
        process.Start();

        process.StandardInput.WriteLine(cmdLine);
        //Giải phóng bộ đệm
        process.StandardInput.Flush();
        process.StandardInput.Close();
        process.WaitForExit();

        string result = process.StandardOutput.ReadToEnd();
        
        return result;
    }
}