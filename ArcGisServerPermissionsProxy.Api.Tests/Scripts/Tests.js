/// <reference path="jasmine.js" />
/// <reference path="dojo/dojo.js" />

require(["dojo/request"], function (request) {
    describe("CanSendEmailWithPost", function () {
        it("can post parameters", function () {
            var data, parameters = JSON.stringify(
                    {
                        application:"test",
                        adminEmails:["sgourley@utah.gov"]
                    });

            runs(function () {
                request.post("http://arcgissecurity/api/admin/createapplication",
                     {
                         //CORS does not allow this to be set so the test fails
                         headers: {
                             "Content-Type": "application/json"
                         },
                         data: parameters,
                         handleAs: "json"
                     }).then(function (dataIn) {
                         data = dataIn;
                         console.log('success');
                     }, function (err) {
                         data = err;
                         console.log('error function');
                         console.dir(err);
                     });
            });

            waitsFor(function () {
                return data;
            });

            runs(function () {
                console.dir(data);
                expect(data).toBeDefined();
                expect(data.status).toEqual(201);
            });
        });
    });
});