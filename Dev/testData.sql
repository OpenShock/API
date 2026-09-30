-- Insert test user with password OpenShock123!

INSERT INTO public.users (id, name, email, password_hash, activated_at, roles, created_at)
VALUES ('50e14f43-dd4e-412f-864d-78943ea28d91', 
        'OpenShock-Test', 
        'test@openshock.org', 
        'bcrypt:$2a$11$bCkcqpsNgFt1.DB33OuLhOsqVbDUp.BIvKVOIYvEO8Hyf26fV6B4y', --- OpenShock123!
        now(),
        ARRAY['admin']::role_type[],
        now());

INSERT INTO public.devices (id, owner_id, name, token, created_at)
VALUES ('7472cba2-6037-488f-b5aa-53b1c39fe450',
        '50e14f43-dd4e-412f-864d-78943ea28d91',
        'Test Hub',
        'ro6DglfhzM@hH1*P5&TOBsY4ipLMSEI4CbY!yNit4V%W&nO*Z9N@H$JzO$mh3D2PvpKL7Sde#6azOs7lBCQq0CovcCg#pX*m&Gt^4S$gCDP@f8eBPB8*q^q*dgdECXKRro6DglfhzM@hH1*P5&TOBsY4ipLMSEI4CbY!yNit4V%W&nO*Z9N@H$JzO$mh3D2PvpKL7Sde#6azOs7lBCQq0CovcCg#pX*m&Gt^4S$gCDP@f8eBPB8*q^q*dgdECXKR',
        now());


INSERT INTO public.shockers (id, name, rf_id, device_id, model, is_paused, created_at)
VALUES ('f73b3d99-44f4-4fbc-9e23-17a310202b07',
        'Test Shocker',
        12345,
        '7472cba2-6037-488f-b5aa-53b1c39fe450',
        'caiXianlin'::shocker_model_type,
        false,
        now());