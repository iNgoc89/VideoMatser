﻿// <auto-generated> This file has been auto generated by EF Core Power Tools. </auto-generated>
#nullable disable
using System;
using System.Collections.Generic;

namespace FFmpegWebAPI.Models;

public partial class CameraRecognition
{
    public int Id { get; set; }

    public int CameraId { get; set; }

    public float RecognitionTime { get; set; }

    public string VehicleCode { get; set; }

    public string VehicleImageUri { get; set; }

    public string PlateImageUri { get; set; }

    public DateTime CreatedDate { get; set; }

    public virtual Camera Camera { get; set; }
}