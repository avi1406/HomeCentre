create view vw_user_list_item as
select u.id,
       u.name,
       u.email,
       u.mobile,
       u.isactive,
       u.genderid
from appuser u;

create view vw_labels as
select l.id,
       l.name,
       l.description,
       l.languageid,
       la.name as language,
       l.isactive
from label l
         join language la
              on l.languageid = la.id;


create view vw_planogramrecordlist as
  SELECT
    distinct on(u.id) u.id,
    u.departmentid,
    u.planocode AS PlanoOrFamilyCode,
    u.status AS PlanoOrFamilyCodeStatus,
	to_char(u.datecreated::date, 'dd/MM/yyyy') AS datecreated,
    to_char(u.rmsintegrationdate :: date, 'dd/MM/yyyy') AS PlanoOrFamilyCodeUpdatedDate,
    to_char(u.firstlevelapproveddate::date, 'dd/MM/yyyy') AS L1ActionDate,
    u.description,
    u.firstlevelcomments,	
	u.l1rejectdescription as L1RejectDescription,
    COALESCE(u.firstlevelaction, 'Pending'::text) AS PlanoOrFamilyNameCreationStatusL1,
    COALESCE(u.secondlevelaction, 'Pending'::text) AS PlanoOrFamilyNameCreationStatusL2,
    to_char(u.secondlevelapproveddate::date, 'dd/MM/yyyy') AS L2ActionDate,
    u.secondlevelcomments,
	u.l2rejectdescription as L2RejectDescription,
    to_char(u.datecreated::date, 'dd/MM/yyyy') AS PlanoOrFamilyCreatedDate,
    t.action AS test,
    t.id AS taskstepinstanceid,
    w.recordid,
    l1.name AS ActionedbyL1,
    l2.name AS ActionedbyL2,
    di.name AS division,
    c1.name AS PlanoOrFamilyCreatedBy,
    concat(u.planogram, ' ', u.familyname) AS PlanoOrFamilyName,
        CASE
            WHEN u.familyname = ''::text THEN 'PlanogramName'::text
            ELSE 'FamilyName'::text
        END AS RequestType
   FROM uidinitiation u
     JOIN workflowrun w ON u.id = w.recordid
     JOIN taskstepinstance t ON t.workflowrunid = w.id
     LEFT JOIN appuser l1 ON l1.id = u.firstlevelapprovedby
     LEFT JOIN appuser c1 ON c1.id = u.createdby
     LEFT JOIN appuser l2 ON l2.id = u.secondlevelapprovedby
     LEFT JOIN division di ON di.id = u.divisionid
  ORDER BY u.id DESC;

create view vw_products as
select pd.id,
       pd.uiddata,
       pd.columndata,
       pd.errordata,
       pd.uid,
       pd.datecreated,
       pd.createdby,
       pd.isactive,
       pt.producttemplatestatusid,
       p.name as producttemplatestatus
from product pd
         left join producttemplate pt on pd.id = pt.templateid
         left join producttemplatestatus p on pt.producttemplatestatusid = p.id;


create view vw_templates as
select t.*, appuserid
from template t
         join templaterole tr on tr.templateid = t.id
         join appuserrole ar on ar.roleid = tr.roleid;

create view vw_emailcontents as
select appuserid,
       appuser.email,
       string_agg(sentemail.emailcontent,',') as emailcontent 
  from actor 
    join appuser on appuser.id = actor.appuserid  
    join sentemail on sentemail.taskstepinstanceid = actor.taskstepinstanceid 
  where sentemail.status = 1 
  group by appuserid,appuser.email  
  order by appuserid ;


-- insert into template  (departmentid, divisionid, name, templatestatusid, isuidtemplate, isactive, createdby, updatedby)
-- select d.id          as departmentid,
--        d.id          as divisionid,
--        'UidTemplate' as name,
--        2             as templatestatusid,
--        true          as isuidtemplate,
--        true          as isactive,
--        1             as createdby,
--        1             as updatedby
-- from division d
--          left join template t on d.id = t.divisionid and t.name = 'UidTemplate' where t.id is null;

create view vw_product_datarequest_items as
select p.Id, p.uid, p.columndata,p.qcstatusid, dri.datarequestid,p.datecreated,p.isactive from product p join datarequestitem dri on dri.productid = p.id;