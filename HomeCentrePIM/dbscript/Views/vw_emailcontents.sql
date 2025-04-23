CREATE OR REPLACE VIEW public.vw_emailcontents
 AS
 SELECT actor.appuserid,
    appuser.email,
    string_agg(sentemail.emailcontent, ','::text) AS emailcontent
   FROM actor
     JOIN appuser ON appuser.id = actor.appuserid
     JOIN sentemail ON sentemail.taskstepinstanceid = actor.taskstepinstanceid
  WHERE sentemail.status = 1
  GROUP BY actor.appuserid, appuser.email
  ORDER BY actor.appuserid;

ALTER TABLE public.vw_emailcontents
    OWNER TO lmdev_admin;