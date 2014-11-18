/*==============================================================*/
/* DBMS name:      MySQL 4.0                                    */
/* Created on:     2014/11/11 23:10:09                          */
/*==============================================================*/


drop table if exists ApkCenter;

drop table if exists AppComment;

drop table if exists AppStatistics;

/*==============================================================*/
/* Table: ApkCenter                                             */
/*==============================================================*/
create table ApkCenter
(
   AppID                          nvarchar(255)                  not null,
   AppName                        nvarchar(32)                   not null default '0.1',
   PackName                       varchar(128)                   not null,
   VersionCode                    float                          not null default 0.1,
   VersionName                    varchar(16)                    not null,
   AppIcon                        varchar(255)                   not null,
   Comment                        nvarchar(999)                  not null,
   UpdateComment                  nvarchar(999),
   AppScreenshot                  nvarchar(999),
   AppDownloadUrl                 varchar(1024)                  not null,
   AppCategory                    varchar(36),
   AppDownloadCount               int                            not null default 0,
   primary key (AppID)
)comment = 'APK应用中心';

/*==============================================================*/
/* Index: Index_1                                               */
/*==============================================================*/
create index Index_1 on ApkCenter
(
   VersionCode,
   AppName
);

/*==============================================================*/
/* Table: AppComment                                            */
/*==============================================================*/
create table AppComment
(
   AcId                           int                            not null,
   AppID                          nvarchar(255)                  not null,
   Username                       nvarchar(255),
   IpAddress                      varchar(18),
   Commnet                        nvarchar(120),
   ReCommentId                    int,
   primary key (AcId)
);

/*==============================================================*/
/* Table: AppStatistics                                         */
/*==============================================================*/
create table AppStatistics
(
   AsId                           int                            not null,
   AppID                          nvarchar(255)                  not null,
   IpAddress                      varchar(18)                    not null,
   primary key (AsId)
);

