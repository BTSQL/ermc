ITRM DB
(DESCRIPTION =(ADDRESS =(PROTOCOL = TCP)(HOST = 150.149.49.131)(PORT = 1539))(CONNECT_DATA =(SERVICE_NAME = ITRP)))
ZMET
ZIMTERTM

이영주()/DA/SKT 님의 말
과장님~~~  오후 03:21
SELECT * FROM ZMET_BPM ;
SELECT * FROM ZMET_BPM_JOBCD ;
SELECT * FROM ZMET_BPM_JOB_GRP_MPNG ;
요 테이블에 정보들이 있는거 같아요
한글 매핑정보요~



요청서 보는 sql
select a.OS_CLL_NO REQUESTID,b.DEF_NM PROCESSTYPE, b.PRC_NM TYPE, 
a.PROCINSTNM TITLE ,c.STRD_HAN_NM RQSTR,substr(a.INIT_DTM,1,8) RQSTDTM
from zmet_bpm a, ZMET_BPM_JOBCD b, ZMET_USER_DTL c
where a.JOB_DS_CD = b.JOB_DS_CD
and a.def_cd = b.def_cd
and a.PRC_CD = b.PRC_CD
and a.TD_YN = 'Y'
and b.JOB_DS_CD = '05'
and b.DEF_CD in  ('0503', '0504')
and b.PRC_CD in ('A00003','A00005','A00006','A00007')
and a.PRC_RGST_EMP_ID = c.USER_MET_ID;
and OS_CLL_NO = 'CHG000000154182';