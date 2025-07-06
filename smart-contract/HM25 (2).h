/*********************************************************************************************/
/* HACKATON: Raise Your Hack (4-8 July 2025)                                                 */
/* TEAM: Kairos                                                                              */
/* ABSTRACT: This contract is specifically designed for the development of our Qbuild audit  */
/*           tool. The objective is to identify potential problems, vulnerabilities and DoS, */
/*           as well as being able to force them by sending the appropriate parameters from  */
/*           Qbuild web page.                                                                */
/*********************************************************************************************/

using namespace QPI;

struct HM252
{
};

struct HM25 : public ContractBase
{
public:

// PROCEDURES

    struct proc01_input{};
    struct proc01_output{};
    
    struct proc02_input{};
    struct proc02_output{};
    
    struct proc03_input{};
    struct proc03_output{};
    
    struct proc04_input{};
    struct proc04_output{};
    
    struct proc05_input{};
    struct proc05_output{};

// FUNCTIONS

    struct func01_input
    {
      sint64 input1;
    };
    struct func01_output
    {
        uint8 output1;
    };
    
    struct func02_input{
        sint8 input2;
    };
    struct func02_output
    {
        uint8 output2;
    };

    struct func03_input{
        sint64 input31;
        char* input32;
    };
    struct func03_output
    {
        uint64 output3;
    };

    struct func04_input{
        bool input4;
    };
    struct func04_output
    {
        bool output4;
    };

    struct func05_input{
        char* input51;
        uint64 input52;
    };
    struct func05_output
    {
        uint8 var5;
    };

private:

// We define uint8 to facilitate bad things happening...
    uint8 var0;
    uint8 var1;
    uint8 var2;
    uint8 var3;
    uint8 var4;
    uint8 var5;

    PUBLIC_PROCEDURE(proc01) 
        //state.var1 = input.input1;
   _

    PUBLIC_PROCEDURE(proc02) // Infinite loop until overflow
        while (state.var2>0)
            state.var2 = state.var2 * 1000000;
   _

    PUBLIC_PROCEDURE(proc03) // Divide by zero
        state.var3 = state.var1 / state.var0;
   _

    PUBLIC_PROCEDURE(proc04) // Infinite call between proc04 and proc05
        state.var4++;
        // proc05();
   _

    PUBLIC_PROCEDURE(proc05)
        state.var5++;
        // proc04();
   _

    PUBLIC_FUNCTION(func01)  // Overflow risk, input1 parameter is sint64 and var1 is uint8
        state.var1 = input.input1;
    _

    PUBLIC_FUNCTION(func02)  // Infinite loop until overflow
        while (state.var2>0)
            output.var2 += state.var2 * 1000000;
    _

    PUBLIC_FUNCTION(func03) // Divide by zero
        output.var3 = state.var3 / state.var0;
    _

    PUBLIC_FUNCTION(func04) // Infinite call between func04 and func05
        output.var4++;
        return qpi.func1(var4);
        //return func05();
    _

    PUBLIC_FUNCTION(func05)
        output.var5++;
        //return func04();
    _

    REGISTER_USER_FUNCTIONS_AND_PROCEDURES

        REGISTER_USER_PROCEDURE(proc01, 1);
        REGISTER_USER_PROCEDURE(proc02, 2);
        REGISTER_USER_PROCEDURE(proc03, 3);
        REGISTER_USER_PROCEDURE(proc04, 4);
        REGISTER_USER_PROCEDURE(proc05, 5);

        REGISTER_USER_FUNCTION(func01, 1);
        REGISTER_USER_FUNCTION(func02, 2);
        REGISTER_USER_FUNCTION(func03, 3);
        REGISTER_USER_FUNCTION(func04, 4);
        REGISTER_USER_FUNCTION(func05, 5);
    _

    INITIALIZE
        state.var0 = 0;
        state.var1 = 1;
        state.var2 = 2;
        state.var3 = 3;
        state.var4 = 4;
        state.var5 = 5;
    _
};
