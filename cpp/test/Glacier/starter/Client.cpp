// **********************************************************************
//
// Copyright (c) 2001
// MutableRealms, Inc.
// Huntsville, AL, USA
//
// All Rights Reserved
//
// **********************************************************************

#include <Ice/Application.h>
#include <Glacier/Glacier.h>
#include <TestCommon.h>
#include <CallbackI.h>

using namespace std;
using namespace Ice;

class CallbackClient : public Application
{
public:

    virtual int run(int, char*[]);
};

int
main(int argc, char* argv[])
{
    CallbackClient app;
    return app.main(argc, argv);
}

int
CallbackClient::run(int argc, char* argv[])
{
    string ref;

    cout << "creating and activating callback receiver adapter... " << flush;
    ObjectAdapterPtr adapter = communicator()->createObjectAdapterWithEndpoints("CallbackReceiverAdapter", "default");
    adapter->activate();
    cout << "ok" << endl;

    cout << "creating and adding callback receiver object... " << flush;
    CallbackReceiverI* callbackReceiverImpl = new CallbackReceiverI;
    ObjectPtr callbackReceiver = callbackReceiverImpl;
    adapter->add(callbackReceiver, stringToIdentity("callbackReceiver"));
    cout << "ok" << endl;

    cout << "testing stringToProxy for router starter... " << flush;
    ref = "Glacier#starter:default -p 12346 -t 2000";
    ObjectPrx starterBase = communicator()->stringToProxy(ref);
    cout << "ok" << endl;

    cout << "testing checked cast for router starter... " << flush;
    Glacier::StarterPrx starter = Glacier::StarterPrx::checkedCast(starterBase);
    test(starter);
    cout << "ok" << endl;

    cout << "starting up router... " << flush;
    RouterPrx router;
    try
    {
	router = starter->startRouter("", "");
    }
    catch (const Glacier::CannotStartRouterException& ex)
    {
	cerr << appName() << ": " << ex << ":\n" << ex.reason << endl;
	return EXIT_FAILURE;
    }
    catch (const Glacier::InvalidPasswordException& ex)
    {
	cerr << appName() << ": " << ex << endl;
	return EXIT_FAILURE;
    }
    test(router);
    cout << "ok" << endl;

    cout << "pinging router... " << flush;
    router->ice_ping();
    cout << "ok" << endl;

    cout << "installing router... " << flush;
    communicator()->setDefaultRouter(router);
    adapter->addRouter(router);
    cout << "ok" << endl;

    cout << "testing stringToProxy... " << flush;
    ref = "callback:default -p 12345 -t 2000";
    ObjectPrx base = communicator()->stringToProxy(ref);
    cout << "ok" << endl;

    cout << "testing checked cast... " << flush;
    CallbackPrx twoway = CallbackPrx::checkedCast(base->ice_twoway()->ice_timeout(-1)->ice_secure(false));
    test(twoway);
    cout << "ok" << endl;

    CallbackReceiverPrx twowayR = CallbackReceiverPrx::uncheckedCast(
	adapter->createProxy(stringToIdentity("callbackReceiver")));
	
    {
	cout << "testing callback... " << flush;
	Context context;
	context["_fwd"] = "t";
	twoway->initiateCallback(twowayR, context);
	test(callbackReceiverImpl->callbackOK());
	cout << "ok" << endl;
    }

    cout << "testing server shutdown... " << flush;
    twoway->shutdown();
    // No ping, otherwise the glacier router prints a warning
    // message if it's started with --Ice.ConnectionWarnings.
    cout << "ok" << endl;
    /*
    try
    {
	twoway->ice_ping();
	test(false);
    }
    // If we use the router, the exact exception reason gets lost.
    //catch(const ConnectFailedException&)
    catch(const UnknownLocalException&)
    {
	cout << "ok" << endl;
    }
    */

    cout << "shutting down router... " << flush;
    router->shutdown();
    try
    {
	router->ice_ping();
	test(false);
    }
    catch(const ConnectFailedException&)
    {
	cout << "ok" << endl;
    }

    return EXIT_SUCCESS;
}
