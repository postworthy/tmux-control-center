#include <errno.h>
#include <sys/file.h>

/* The daemon owns inherited descriptor 9. Lock its shared open-file description;
 * exiting this helper does not release the parent's descriptor/lock. */
int main(void)
{
    if (flock(9, LOCK_EX | LOCK_NB) == 0) return 0;
    return errno == EWOULDBLOCK ? 1 : 2;
}
