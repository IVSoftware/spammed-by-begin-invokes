Running the repro code in the post, what stuck me is how this smells like a hardware overrun and I asked myself how this could be. In the process of experimenting I, too, noticed how repeatable and stable the 10000 threshold is, not "moving around" the way certain race conditions might. The idea becomes finding the canonical source of it, which I believe I found in the registry at: 

`Computer\HKEY_LOCAL_MACHINE\SOFTWARE\Microsoft\Windows NT\CurrentVersion\Windows` 

where the key is:

`USERPostMessageLimit`.

This seems to lie at the heart of the matter. I went ahead and edited it to 20000, restarted the PC, and confirmed in the repro code that the threshold tracked the new value.

The nature of this being an OS value in the registry has me draw some preliminary conclusions:

- The behavior would be consistent with an `alloc` of a block of unmanaged physical memory. For all intents and purposes, this "is" a hardware register. 
- It's a fair and reasonable assumption that `USERPostMessageLimit` would be a queue, not a stack, and that if it were overrun then the head and tail of this circular buffer could either overlap or more likely just start throwing messages in the bit bucket (the overlap is disallowed). 
- So, a plausible explanation is that this buffer fills up so quickly that the earlier messages haven't dequeued and are therefore irretrievably lost.
- This jibes with my observations, that even though the intermittent "debug message" was not called in the case of exceeding the limit, the app seemed to remain viable and healthy in all other respects.
___

My test engineering spidey senses tell me there is some better way to observe this timing, but I'm still ironing out the specifics.