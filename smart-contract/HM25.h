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

    struct proc01_input {};
    struct proc01_output
    {
        uint8 output1;
    };
    
    struct proc02_input{};
    struct proc02_output
    {
        uint8 output21;
        bool output22;
    };

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
        uint64 input2;
    };
    struct func02_output
    {
        uint8 output2;
    };

    struct func03_input{
        sint64 input3;
    };
    struct func03_output
    {
        uint8 output3;
    };

private:

    uint8 var1;
    uint8 var2;
    uint8 var3;

    PUBLIC_PROCEDURE(proc01) // Risk: Divide by zero
        output.output1 = state.var1 / state.var2;
   _

    PUBLIC_PROCEDURE(proc02) // Risk: Overflow
        while (state.var2 < state.var1)
            state.var2 = state.var2 * 1000;
        output.output21 = state.var2;
        output.output22 = true;
   _

    PUBLIC_FUNCTION(func01)  // Risk: Overflow (input1 parameter is sint64 and output1 is uint8)
        output.output1 = input.input1;
    _

    PUBLIC_FUNCTION(func02)  // Risk: Infinite loop until output2 overflow
        for (output.output2 = 0; output.output2 < input.input2; output.output2 += 10000);
    _

    PUBLIC_FUNCTION(func03) // Riks: Divide by zero, overflow
        output.output3 = state.var3 / input.input3;
    _

    REGISTER_USER_FUNCTIONS_AND_PROCEDURES

        REGISTER_USER_PROCEDURE(proc01, 1);
        REGISTER_USER_PROCEDURE(proc02, 2);

        REGISTER_USER_FUNCTION(func01, 1);
        REGISTER_USER_FUNCTION(func02, 2);
        REGISTER_USER_FUNCTION(func03, 3);
    _

    INITIALIZE
        state.var1 = 1;
        // state.var2 = 2; Risk: Non initialized variable
        state.var3 = 3;
    _
};
