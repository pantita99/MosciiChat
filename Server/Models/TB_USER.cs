﻿// <auto-generated> This file has been auto generated by EF Core Power Tools. </auto-generated>
#nullable disable
using System;
using System.Collections.Generic;

namespace Server.Models;

public partial class TB_USER
{
    public string USER_ID { get; set; }

    public string ORGID { get; set; }

    public string PASSWORD { get; set; }

    public int DEP_ID { get; set; }

    public int? LOCATION_ID { get; set; }

    public string FIRSTNAME { get; set; }

    public string LASTNAME { get; set; }

    public string EMAIL { get; set; }

    public string ADDRESS { get; set; }

    public string PHONEWORK { get; set; }

    public string PHONEHOME { get; set; }

    public string PHONEMOBILE { get; set; }

    public string JOBFUNCTION { get; set; }

    public string DESCRIPTION { get; set; }

    public string EXT1 { get; set; }

    public string EXT2 { get; set; }

    public DateTime? DATECREATED { get; set; }

    public string CITIZENID { get; set; }

    public string DRIVINGLICENSE { get; set; }

    public byte[] IMAGE { get; set; }

    public DateTime? DATEMODIFILED { get; set; }

    public bool? ActiveDirectory { get; set; }

    public string ManagerUserID { get; set; }

    public string ManagerLavel { get; set; }

    public int? EMPID { get; set; }

    public bool? UserActive { get; set; }

    public int? SectionID { get; set; }

    public int? SubSectionID { get; set; }

    public int? DivisionID { get; set; }

    public string ManagerUserID2 { get; set; }

    public string ManagerUserID3 { get; set; }

    public string ManagerLevel { get; set; }

    public string SecondaryDeputy { get; set; }

    public string LineID { get; set; }

    public long? FK_BUILDING { get; set; }

    public long? FK_FLOOR { get; set; }

    public long? FK_ROOM { get; set; }
}